import json
import sqlite3
import tempfile
import time
import unittest
from contextlib import closing
from pathlib import Path
from types import SimpleNamespace
from unittest.mock import Mock, patch

from node_observations import load_node_database, merge_node_record
from node_stats_mqtt import MqttSettings, parse_broker_address, sample_mqtt
from update_node_stats import read_meshmonitor_counts, update


class NodeStatsTests(unittest.TestCase):
    def setUp(self):
        working = tempfile.TemporaryDirectory()
        self.addCleanup(working.cleanup)
        self.data = Path(working.name)
        self.history = self.data / "node-database.json"
        self.stats = self.data / "node-stats.json"
        self.meshmonitor = self.data / "meshmonitor.db"

    def create_meshmonitor(self, rows):
        with closing(sqlite3.connect(self.meshmonitor)) as connection, connection:
            connection.execute("CREATE TABLE nodes (nodeId, nodeNum, lastHeard)")
            connection.executemany("INSERT INTO nodes VALUES (?, ?, ?)", rows)

    def read_snapshot(self):
        return json.loads(self.stats.read_text())

    @patch("update_node_stats.sample_mqtt", side_effect=ConnectionRefusedError)
    def test_meshmonitor_snapshot_is_published_during_broker_outage(self, _sample):
        self.create_meshmonitor([("!00000001", 1, int(time.time()))])
        self.assertEqual(update(self.data, self.history, self.meshmonitor), 0)
        self.assertEqual(self.read_snapshot()["source"], "meshmonitor-db")
        self.assertEqual(self.read_snapshot()["nodes30Min"], "1")
        self.assertFalse(self.history.exists())

    def test_meshmonitor_windows_use_one_row_per_node_and_a_common_cutoff(self):
        now = 1800000000
        self.create_meshmonitor([
            ("!aabbccdd", None, now - 86401),
            ("!AABBCCDD", None, now - 1800),
            (None, 2, now - 7200),
            ("!00000003", 3, now - 86400),
            ("!00000004", 4, now - 86401),
            (None, None, now),
        ])
        self.assertEqual(read_meshmonitor_counts(self.meshmonitor, now), {
            "totalNodes": 4, "nodes30Min": 1, "nodes2Hr": 2, "nodes24Hr": 3,
        })

    @patch("update_node_stats.sample_mqtt", return_value=("example.test:1883", 0))
    def test_meshmonitor_snapshot_is_published_when_mqtt_history_cannot_be_saved(self, _sample):
        self.create_meshmonitor([("!00000001", 1, int(time.time()))])
        self.history.mkdir()
        self.assertEqual(update(self.data, self.history, self.meshmonitor), 0)
        self.assertEqual(self.read_snapshot()["source"], "meshmonitor-db")

    def test_mqtt_history_stays_warm_with_meshmonitor_and_is_used_when_it_fails(self):
        self.create_meshmonitor([("!00000001", 1, int(time.time()))])
        (self.data / "node-observations.json").write_text(
            json.dumps({"nodes": {"2": {"lastSeen": 1750000000}}})
        )

        def sample(_settings, history):
            merge_node_record(history, "!00000003", int(time.time()))
            return "example.test:1883", 1

        with patch("update_node_stats.sample_mqtt", side_effect=sample):
            self.assertEqual(update(self.data, self.history, self.meshmonitor), 0)
            self.assertEqual(self.read_snapshot()["source"], "meshmonitor-db")
            self.assertEqual(set(load_node_database(self.data, self.history, int(time.time()))),
                             {"!00000002", "!00000003"})
            self.assertEqual(update(self.data, self.history, None), 0)
        self.assertEqual(self.read_snapshot()["source"], "mqtt")
        self.assertEqual(self.read_snapshot()["totalNodes"], "2")

    @patch("update_node_stats.sample_mqtt", side_effect=ConnectionRefusedError)
    def test_two_failed_sources_keep_the_previous_snapshot(self, _sample):
        previous = '{"totalNodes":"4","updatedAtUtc":"2024-01-01T12:00:00Z"}'
        self.stats.write_text(previous)
        self.assertEqual(update(self.data, self.history, None), 1)
        self.assertEqual(self.stats.read_text(), previous)

    def test_undated_tsv_seed_does_not_refresh_known_nodes(self):
        previous = 1750000000
        now = previous + 50000
        self.history.write_text(json.dumps({"nodes": {"1": {"firstSeen": previous, "lastSeen": previous}}}))
        (self.data / "node-history.tsv").write_text("!00000001\n!00000002\n")
        history = load_node_database(self.data, self.history, now)
        self.assertEqual(history["!00000001"]["lastSeen"], previous)
        self.assertEqual(history["!00000002"]["lastSeen"], now)

    def test_tls_infers_from_url_with_blank_override_and_preserves_explicit_false(self):
        with patch.dict("os.environ", {"MQTT_STATS_TLS": "", "MQTT_STATS_BROKER_URL": "mqtts://example.test"}):
            settings = MqttSettings.from_environment()
            self.assertIsNone(settings.tls)
            self.assertEqual(parse_broker_address(settings.broker), ("example.test", 8883, True))
        with patch.dict("os.environ", {"MQTT_STATS_TLS": "false"}):
            self.assertEqual(MqttSettings.from_environment().tls, "false")

    def test_broker_disconnect_after_suback_is_a_failed_sample(self):
        client = Mock()
        module = SimpleNamespace(
            CallbackAPIVersion=SimpleNamespace(VERSION2=2),
            Client=Mock(return_value=client),
        )
        package = SimpleNamespace(client=module)

        def accept_then_disconnect():
            client.on_subscribe(client, None, 1, [SimpleNamespace(is_failure=False)], None)
            client.on_disconnect(client, None, None, "connection lost", None)

        client.loop_start.side_effect = accept_then_disconnect
        settings = MqttSettings("mqtt://example.test", "msh/US/#", "", "", "fixture", 60, 10, None)
        with patch.dict("sys.modules", {
            "paho": SimpleNamespace(mqtt=package),
            "paho.mqtt": package,
            "paho.mqtt.client": module,
        }):
            with self.assertRaisesRegex(RuntimeError, "disconnected while sampling"):
                sample_mqtt(settings, {})
        client.loop_stop.assert_called_once()


if __name__ == "__main__":
    unittest.main()

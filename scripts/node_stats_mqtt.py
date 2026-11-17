from __future__ import annotations

import json
import math
import os
import re
import ssl
import threading
import time
from dataclasses import dataclass
from urllib.parse import urlparse

from node_observations import merge_node_record, normalize_node_id

try:
    from google.protobuf.message import DecodeError
    from meshtastic.protobuf import mqtt_pb2
except ImportError:
    mqtt_pb2 = None


@dataclass(frozen=True)
class MqttSettings:
    broker: str
    topic: str
    username: str
    password: str
    client_id: str
    sample_seconds: float
    connect_timeout: float
    tls: str | None

    @classmethod
    def from_environment(cls) -> MqttSettings:
        return cls(
            broker=os.environ.get("MQTT_STATS_BROKER_URL") or os.environ.get("MQTT_BROKER_URL") or "mqtt://127.0.0.1:1883",
            topic=os.environ.get("MQTT_STATS_TOPIC") or os.environ.get("MQTT_TOPIC") or "msh/US/#",
            username=os.environ.get("MQTT_STATS_USERNAME") or os.environ.get("MQTT_USERNAME") or "",
            password=os.environ.get("MQTT_STATS_PASSWORD") or os.environ.get("MQTT_PASSWORD") or "",
            client_id=os.environ.get("MQTT_STATS_CLIENT_ID", f"montanamesh-node-stats-{os.getpid()}"),
            sample_seconds=float(os.environ.get("MQTT_STATS_SAMPLE_SECONDS", "60")),
            connect_timeout=float(os.environ.get("MQTT_STATS_CONNECT_TIMEOUT_SECONDS", "15")),
            tls=os.environ.get("MQTT_STATS_TLS", "").strip() or None,
        )


def parse_broker_address(raw: str) -> tuple[str, int, bool]:
    # A bare hostname/port is accepted alongside the URI forms.
    parsed = urlparse(raw if "://" in raw else f"mqtt://{raw}")
    if not parsed.hostname:
        raise ValueError("Missing MQTT broker hostname")
    if parsed.scheme in {"mqtt", "tcp"}:
        return parsed.hostname, parsed.port or 1883, False
    if parsed.scheme in {"mqtts", "ssl", "tls"}:
        return parsed.hostname, parsed.port or 8883, True
    raise ValueError(f"Unsupported MQTT broker URL scheme: {parsed.scheme}")


def node_from_topic(topic: str) -> str | None:
    matches = re.findall(r"!(?P<node>[0-9a-fA-F]{8})(?:/|$)", topic)
    if matches:
        return f"!{matches[-1].lower()}"
    return None


def node_from_json(payload: bytes) -> str | None:
    try:
        decoded = payload.decode("utf-8")
        document = json.loads(decoded)
    except (UnicodeDecodeError, json.JSONDecodeError):
        return None

    if not isinstance(document, dict):
        return None

    for key in ("from", "fromId", "from_id", "sender", "senderId", "sender_id", "node_id", "nodeId"):
        node_id = normalize_node_id(document.get(key))
        if node_id:
            return node_id
    return None


def node_from_protobuf(payload: bytes) -> str | None:
    if mqtt_pb2 is None:
        return None
    envelope = mqtt_pb2.ServiceEnvelope()
    try:
        envelope.ParseFromString(payload)
    except DecodeError:
        return None

    if envelope.HasField("packet"):
        node_id = normalize_node_id(getattr(envelope.packet, "from"))
        if node_id:
            return node_id
    return normalize_node_id(getattr(envelope, "gateway_id", None))


def extract_node_id(topic: str, payload: bytes) -> str | None:
    return node_from_json(payload) or node_from_protobuf(payload) or node_from_topic(topic)


def sample_mqtt(settings: MqttSettings, database: dict[str, dict[str, int]]) -> tuple[str, int]:
    try:
        import paho.mqtt.client as mqtt
    except ImportError as exc:
        raise RuntimeError("paho-mqtt is required to sample MQTT") from exc

    host, port, tls_enabled = parse_broker_address(settings.broker)
    if not math.isfinite(settings.sample_seconds) or not math.isfinite(settings.connect_timeout):
        raise ValueError("MQTT sample and connection durations must be finite")
    if settings.tls is not None:
        tls_enabled = settings.tls.lower() in {"1", "true", "yes", "on"}

    ready = threading.Event()
    interrupted = threading.Event()
    shutting_down = threading.Event()
    rejection = None
    seen: set[str] = set()

    def on_connect(client, _userdata, _flags, reason_code, _properties):
        nonlocal rejection
        if reason_code.is_failure:
            rejection = f"MQTT connect was refused ({reason_code})"
            ready.set()
            return
        result, _ = client.subscribe(settings.topic)
        if result != mqtt.MQTT_ERR_SUCCESS:
            rejection = f"MQTT subscribe failed ({result})"
            ready.set()

    def on_subscribe(_client, _userdata, _mid, reason_codes, _properties):
        nonlocal rejection
        if any(code.is_failure for code in reason_codes):
            rejection = "MQTT subscription was refused"
        ready.set()

    def on_disconnect(_client, _userdata, _flags, reason_code, _properties):
        nonlocal rejection
        if not shutting_down.is_set():
            rejection = f"MQTT disconnected while sampling ({reason_code})"
            interrupted.set()
            ready.set()

    def on_message(_client, _userdata, message):
        node_id = extract_node_id(message.topic, bytes(message.payload))
        if node_id:
            merge_node_record(database, node_id, int(time.time()))
            seen.add(node_id)

    client = mqtt.Client(mqtt.CallbackAPIVersion.VERSION2, client_id=settings.client_id)
    client.on_connect = on_connect
    client.on_subscribe = on_subscribe
    client.on_disconnect = on_disconnect
    client.on_message = on_message
    client.connect_timeout = settings.connect_timeout
    if settings.username:
        client.username_pw_set(settings.username, settings.password or None)
    if tls_enabled:
        client.tls_set(cert_reqs=ssl.CERT_REQUIRED)

    client.connect(host, port, keepalive=max(30, int(settings.sample_seconds) + 10))
    client.loop_start()
    try:
        if not ready.wait(settings.connect_timeout):
            raise RuntimeError(f"Timed out subscribing to MQTT broker {host}:{port}")
        if rejection:
            raise RuntimeError(rejection)
        interrupted.wait(max(1.0, settings.sample_seconds))
    finally:
        shutting_down.set()
        try:
            client.disconnect()
        finally:
            client.loop_stop()

    if rejection:
        raise RuntimeError(rejection)
    return f"{host}:{port}", len(seen)

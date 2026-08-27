#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ENV_FILE="${NODE_STATS_ENV_FILE:-${ROOT_DIR}/../.env}"
DATA_DIR="${NODE_STATS_DATA_DIR:-${ROOT_DIR}/data}"
STATS_FILE="${DATA_DIR}/node-stats.json"
DATABASE_FILE="${NODE_STATS_DATABASE_FILE:-${NODE_STATS_STATE_FILE:-${DATA_DIR}/node-database.json}}"
TMP_STATS="${DATA_DIR}/.node-stats.tmp"
TMP_DATABASE="${DATA_DIR}/.node-database.tmp"

mkdir -p "${DATA_DIR}"

if [[ -f "${ENV_FILE}" ]]; then
  # Safely load KEY=VALUE lines from .env without executing shell code.
  while IFS= read -r line || [[ -n "${line}" ]]; do
    [[ -z "${line}" ]] && continue
    [[ "${line}" =~ ^[[:space:]]*# ]] && continue
    [[ "${line}" != *=* ]] && continue

    key="${line%%=*}"
    value="${line#*=}"

    key="${key#"${key%%[![:space:]]*}"}"
    key="${key%"${key##*[![:space:]]}"}"

    value="${value#"${value%%[![:space:]]*}"}"
    value="${value%"${value##*[![:space:]]}"}"

    if [[ "${value}" =~ ^\".*\"$ ]]; then
      value="${value:1:${#value}-2}"
    elif [[ "${value}" =~ ^\'.*\'$ ]]; then
      value="${value:1:${#value}-2}"
    fi

    if [[ "${key}" =~ ^[A-Za-z_][A-Za-z0-9_]*$ && -z "${!key+x}" ]]; then
      export "${key}=${value}"
    fi
  done < "${ENV_FILE}"
fi

if ! command -v python3 >/dev/null 2>&1; then
  echo "python3 is required." >&2
  exit 1
fi

export DATA_DIR STATS_FILE DATABASE_FILE TMP_STATS TMP_DATABASE

python3 - <<'PY'
from __future__ import annotations

import json
import os
import re
import ssl
import time
from datetime import datetime
from pathlib import Path
from urllib.parse import urlparse

try:
    import paho.mqtt.client as mqtt
except ImportError as exc:
    raise SystemExit("paho-mqtt is required. Install with: python3 -m pip install paho-mqtt") from exc

try:
    from meshtastic.protobuf import mqtt_pb2
except ImportError:
    mqtt_pb2 = None


DATA_DIR = Path(os.environ["DATA_DIR"])
STATS_FILE = Path(os.environ["STATS_FILE"])
DATABASE_FILE = Path(os.environ["DATABASE_FILE"])
TMP_STATS = Path(os.environ["TMP_STATS"])
TMP_DATABASE = Path(os.environ["TMP_DATABASE"])

BROKER_URL = os.environ.get("MQTT_STATS_BROKER_URL") or os.environ.get("MQTT_BROKER_URL") or "mqtt://127.0.0.1:1883"
TOPIC = os.environ.get("MQTT_STATS_TOPIC") or os.environ.get("MQTT_TOPIC") or "msh/US/#"
USERNAME = os.environ.get("MQTT_STATS_USERNAME") or os.environ.get("MQTT_USERNAME") or ""
PASSWORD = os.environ.get("MQTT_STATS_PASSWORD") or os.environ.get("MQTT_PASSWORD") or ""
CLIENT_ID = os.environ.get("MQTT_STATS_CLIENT_ID", f"montanamesh-node-stats-{os.getpid()}")
SAMPLE_SECONDS = float(os.environ.get("MQTT_STATS_SAMPLE_SECONDS", "60"))
CONNECT_TIMEOUT = float(os.environ.get("MQTT_STATS_CONNECT_TIMEOUT_SECONDS", "15"))


def env_bool(name: str, default: bool = False) -> bool:
    raw = os.environ.get(name)
    if raw is None:
        return default
    return raw.strip().lower() in {"1", "true", "yes", "on"}


def parse_broker_url(raw: str) -> tuple[str, int, bool]:
    parsed = urlparse(raw)
    if parsed.scheme in {"mqtt", "tcp", ""}:
        return parsed.hostname or raw, parsed.port or 1883, False
    if parsed.scheme in {"mqtts", "ssl", "tls"}:
        return parsed.hostname or raw, parsed.port or 8883, True
    raise SystemExit(f"Unsupported MQTT broker URL scheme: {parsed.scheme}")


def normalize_node_id(value: object) -> str | None:
    if value is None:
        return None
    if isinstance(value, int):
        return f"!{value & 0xFFFFFFFF:08x}"
    text = str(value).strip()
    if not text:
        return None
    if text.startswith("!"):
        return text.lower()
    if text.isdigit():
        return f"!{int(text) & 0xFFFFFFFF:08x}"
    hex_match = re.fullmatch(r"(?:0x)?([0-9a-fA-F]{8})", text)
    if hex_match:
        return f"!{hex_match.group(1).lower()}"
    return None


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
    except Exception:
        return None

    packet = getattr(envelope, "packet", None)
    if packet is not None:
        node_id = normalize_node_id(getattr(packet, "from"))
        if node_id:
            return node_id
    return normalize_node_id(getattr(envelope, "gateway_id", None))


def extract_node_id(topic: str, payload: bytes) -> str | None:
    return node_from_json(payload) or node_from_protobuf(payload) or node_from_topic(topic)


def parse_seen_time(value: object) -> int | None:
    if value is None:
        return None
    if isinstance(value, (int, float)):
        return int(value)
    text = str(value).strip()
    if not text:
        return None
    try:
        return int(float(text))
    except ValueError:
        pass
    if text.endswith("Z"):
        text = text[:-1] + "+00:00"
    try:
        return int(datetime.fromisoformat(text).timestamp())
    except ValueError:
        return None


def format_utc(timestamp: int) -> str:
    return time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime(timestamp))


def load_node_database() -> dict[str, dict[str, int]]:
    candidate_files = [DATABASE_FILE]
    legacy_file = DATA_DIR / "node-observations.json"
    if legacy_file != DATABASE_FILE:
        candidate_files.append(legacy_file)

    raw: object = {}
    for path in candidate_files:
        if not path.exists():
            continue
        try:
            raw = json.loads(path.read_text(encoding="utf-8"))
            break
        except (OSError, json.JSONDecodeError):
            raw = {}

    if not isinstance(raw, dict):
        return {}

    nodes = raw.get("nodes", raw)
    if not isinstance(nodes, dict):
        return {}

    result: dict[str, dict[str, int]] = {}
    for node_id, record in nodes.items():
        normalized = normalize_node_id(node_id)
        if not normalized:
            continue

        if isinstance(record, dict):
            last_seen = parse_seen_time(record.get("lastSeen") or record.get("lastSeenUtc"))
            first_seen = parse_seen_time(record.get("firstSeen") or record.get("firstSeenUtc"))
        else:
            last_seen = parse_seen_time(record)
            first_seen = last_seen

        if last_seen is None:
            continue
        if first_seen is None:
            first_seen = last_seen
        result[normalized] = {"firstSeen": first_seen, "lastSeen": last_seen}
    return result


def write_json_atomic(path: Path, tmp_path: Path, payload: dict[str, object]) -> None:
    tmp_path.write_text(json.dumps(payload, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    tmp_path.replace(path)


now = int(time.time())
node_database = load_node_database()
seen_this_run: set[str] = set()
host, port, tls_enabled = parse_broker_url(BROKER_URL)
tls_enabled = env_bool("MQTT_STATS_TLS", tls_enabled)


def mqtt_reason_ok(reason_code: object) -> bool:
    is_failure = getattr(reason_code, "is_failure", None)
    if isinstance(is_failure, bool):
        return not is_failure
    try:
        return int(reason_code) == 0
    except (TypeError, ValueError):
        return str(reason_code).lower() in {"success", "0"}


def on_connect(client: mqtt.Client, userdata: object, flags: object, reason_code: object, properties: object | None = None) -> None:
    if mqtt_reason_ok(reason_code):
        client.subscribe(TOPIC)
    else:
        print(f"MQTT connect failed with code {reason_code}", flush=True)


def on_message(client: mqtt.Client, userdata: object, message: mqtt.MQTTMessage) -> None:
    node_id = extract_node_id(message.topic, bytes(message.payload))
    if not node_id:
        return
    seen_at = int(time.time())
    record = node_database.setdefault(node_id, {"firstSeen": seen_at, "lastSeen": seen_at})
    record["lastSeen"] = seen_at
    if seen_at < record.get("firstSeen", seen_at):
        record["firstSeen"] = seen_at
    seen_this_run.add(node_id)


client = mqtt.Client(mqtt.CallbackAPIVersion.VERSION2, client_id=CLIENT_ID)
client.on_connect = on_connect
client.on_message = on_message
if USERNAME:
    client.username_pw_set(USERNAME, PASSWORD or None)
if tls_enabled:
    client.tls_set(cert_reqs=ssl.CERT_REQUIRED)

client.connect(host, port, keepalive=max(30, int(SAMPLE_SECONDS) + 10))
client.loop_start()

deadline = time.time() + CONNECT_TIMEOUT
while not client.is_connected() and time.time() < deadline:
    time.sleep(0.1)

if not client.is_connected():
    client.loop_stop()
    raise SystemExit(f"Timed out connecting to MQTT broker {host}:{port}")

time.sleep(max(1.0, SAMPLE_SECONDS))
client.disconnect()
client.loop_stop()

now = int(time.time())
updated = format_utc(now)
last_seen_values = [record["lastSeen"] for record in node_database.values()]

stats = {
    "totalNodes": str(len(node_database)),
    "nodes30Min": str(sum(1 for seen in last_seen_values if seen >= now - 1800)),
    "nodes2Hr": str(sum(1 for seen in last_seen_values if seen >= now - 7200)),
    "nodes24Hr": str(sum(1 for seen in last_seen_values if seen >= now - 86400)),
    "updatedAtUtc": updated,
}
database = {
    "updatedAtUtc": updated,
    "source": "mqtt",
    "broker": f"{host}:{port}",
    "topic": TOPIC,
    "sampleSeconds": SAMPLE_SECONDS,
    "seenThisRun": len(seen_this_run),
    "totalNodes": len(node_database),
    "nodes": {
        node_id: {
            "firstSeenUtc": format_utc(record["firstSeen"]),
            "lastSeenUtc": format_utc(record["lastSeen"]),
        }
        for node_id, record in sorted(node_database.items())
    },
}

write_json_atomic(DATABASE_FILE, TMP_DATABASE, database)
write_json_atomic(STATS_FILE, TMP_STATS, stats)
print(
    f"wrote {STATS_FILE} from MQTT topic {TOPIC}: "
    f"{stats['totalNodes']} total, {stats['nodes30Min']} active in 30m, {len(seen_this_run)} seen this run",
    flush=True,
)
PY

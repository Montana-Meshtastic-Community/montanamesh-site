from __future__ import annotations

import fcntl
import json
import os
import sqlite3
import sys
import tempfile
import time
from contextlib import closing
from pathlib import Path

from node_observations import format_utc, load_node_database
from node_stats_mqtt import MqttSettings, sample_mqtt


WINDOWS = {"nodes30Min": 1800, "nodes2Hr": 7200, "nodes24Hr": 86400}


def read_meshmonitor_counts(path: Path, now: int) -> dict[str, int] | None:
    active_counts = ", ".join(
        f"COUNT(CASE WHEN last_heard >= ? THEN 1 END) AS {name}"
        for name in WINDOWS
    )
    try:
        with closing(sqlite3.connect(f"{path.resolve().as_uri()}?mode=ro", uri=True, timeout=5)) as connection:
            row = connection.execute(
                f"""
                WITH identified AS (
                    SELECT COALESCE(NULLIF(LOWER(TRIM(nodeId)), ''),
                                   CASE WHEN nodeNum IS NOT NULL THEN printf('!%08x', nodeNum & 4294967295) END) AS id,
                           lastHeard
                    FROM nodes
                ), distinct_nodes AS (
                    SELECT id, MAX(lastHeard) AS last_heard
                    FROM identified
                    WHERE id IS NOT NULL
                    GROUP BY id
                )
                SELECT COUNT(*) AS totalNodes, {active_counts} FROM distinct_nodes
                """,
                tuple(now - seconds for seconds in WINDOWS.values()),
            ).fetchone()
    except (OSError, sqlite3.Error) as exc:
        print(f"could not read MeshMonitor node counts from {path}: {exc}", file=sys.stderr, flush=True)
        return None
    return dict(zip(("totalNodes", *WINDOWS), row))


def counts_from_observations(database: dict[str, dict[str, int]], now: int) -> dict[str, int]:
    return {
        "totalNodes": len(database),
        **{
            name: sum(record["lastSeen"] >= now - duration for record in database.values())
            for name, duration in WINDOWS.items()
        },
    }


def write_json_atomic(path: Path, payload: dict[str, object]) -> None:
    with tempfile.NamedTemporaryFile(mode="w", encoding="utf-8", dir=path.parent, prefix=f".{path.stem}.", suffix=".tmp", delete=False) as stream:
        temp_path = Path(stream.name)
        try:
            stream.write(json.dumps(payload, indent=2, sort_keys=True) + "\n")
            stream.close()
            temp_path.chmod(0o644)
            temp_path.replace(path)
        finally:
            temp_path.unlink(missing_ok=True)


def update(data_dir: Path, database_path: Path, meshmonitor_path: Path | None) -> int:
    now = int(time.time())
    database = load_node_database(data_dir, database_path, now)

    try:
        settings = MqttSettings.from_environment()
        broker, seen = sample_mqtt(settings, database)
    except (OSError, ValueError, RuntimeError) as exc:
        mqtt_sample = None
        print(f"could not sample MQTT: {exc}", file=sys.stderr, flush=True)
    else:
        mqtt_sample = {
            "broker": broker,
            "topic": settings.topic,
            "sampleSeconds": settings.sample_seconds,
            "seenThisRun": seen,
        }

    now = int(time.time())
    updated = format_utc(now)
    counts = read_meshmonitor_counts(meshmonitor_path, now) if meshmonitor_path else None
    if counts is not None:
        source = "meshmonitor-db"
    elif mqtt_sample is not None:
        counts = counts_from_observations(database, now)
        source = "mqtt"
    else:
        print("No fresh node statistics are available. Keeping the previous snapshot.", file=sys.stderr, flush=True)
        return 1

    if mqtt_sample is not None:
        history = {
            **mqtt_sample,
            "updatedAtUtc": updated,
            "source": source,
            "meshmonitorDbPath": str(meshmonitor_path) if source == "meshmonitor-db" else None,
            "totalNodes": len(database),
            "nodes": {
                node_id: {
                    "firstSeenUtc": format_utc(record["firstSeen"]),
                    "lastSeenUtc": format_utc(record["lastSeen"]),
                }
                for node_id, record in sorted(database.items())
            },
        }
        try:
            database_path.parent.mkdir(parents=True, exist_ok=True)
            write_json_atomic(database_path, history)
        except OSError as exc:
            print(f"could not save MQTT observation history: {exc}", file=sys.stderr, flush=True)
            if source == "mqtt":
                return 1

    stats_path = data_dir / "node-stats.json"
    write_json_atomic(stats_path, {
        **{name: str(count) for name, count in counts.items()},
        "updatedAtUtc": updated,
        "source": source,
    })
    print(f"wrote {stats_path}: {counts['totalNodes']} total ({source}), {counts['nodes30Min']} active in 30m", flush=True)
    return 0


def main() -> int:
    data_dir = Path(os.environ.get("NODE_STATS_DATA_DIR") or Path(__file__).resolve().parents[1] / "data")
    database_path = Path(
        os.environ.get("NODE_STATS_DATABASE_FILE") or os.environ.get("NODE_STATS_STATE_FILE") or data_dir / "node-database.json"
    )
    meshmonitor = os.environ.get("MESHMONITOR_DB_PATH", "").strip()
    meshmonitor_path = Path(meshmonitor).expanduser() if meshmonitor else None
    data_dir.mkdir(parents=True, exist_ok=True)
    with (data_dir / ".node-stats.lock").open("a") as lock:
        fcntl.flock(lock, fcntl.LOCK_EX)
        return update(data_dir, database_path, meshmonitor_path)


if __name__ == "__main__":
    raise SystemExit(main())

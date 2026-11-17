from __future__ import annotations

import json
import re
import time
from datetime import datetime, timezone
from pathlib import Path


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
        try:
            return f"!{int(text) & 0xFFFFFFFF:08x}"
        except ValueError:
            return None
    hex_match = re.fullmatch(r"(?:0x)?([0-9a-fA-F]{8})", text)
    if hex_match:
        return f"!{hex_match.group(1).lower()}"
    return None


def parse_seen_time(value: object) -> int | None:
    if value is None:
        return None
    text = str(value).strip()
    try:
        numeric = value if isinstance(value, int) else int(float(text))
    except (ValueError, OverflowError):
        if text.endswith("Z"):
            text = text[:-1] + "+00:00"
        try:
            numeric = int(datetime.fromisoformat(text).timestamp())
        except (ValueError, OverflowError, OSError):
            return None
    try:
        datetime.fromtimestamp(numeric, timezone.utc)
    except (ValueError, OverflowError, OSError):
        return None
    return numeric


def format_utc(timestamp: int) -> str:
    return time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime(timestamp))


def merge_node_record(database: dict[str, dict[str, int]], node_id: str, seen_at: int) -> None:
    record = database.setdefault(node_id, {"firstSeen": seen_at, "lastSeen": seen_at})
    record["firstSeen"] = min(record.get("firstSeen", seen_at), seen_at)
    record["lastSeen"] = max(record.get("lastSeen", seen_at), seen_at)


def merge_node_history(database: dict[str, dict[str, int]], history_file: Path, now: int) -> None:
    if not history_file.exists():
        return

    try:
        lines = history_file.read_text(encoding="utf-8").splitlines()
    except (OSError, UnicodeDecodeError):
        return

    for line in lines:
        parts = line.split()
        if not parts:
            continue
        node_id = normalize_node_id(parts[0])
        if not node_id:
            continue
        seen_at = parse_seen_time(parts[1]) if len(parts) > 1 else None
        if seen_at is not None:
            merge_node_record(database, node_id, seen_at)
        elif node_id not in database:
            merge_node_record(database, node_id, now)


def load_node_database(data_dir: Path, database_file: Path, now: int) -> dict[str, dict[str, int]]:
    candidate_files = [database_file]
    legacy_file = data_dir / "node-observations.json"
    if legacy_file != database_file:
        candidate_files.append(legacy_file)

    nodes: dict[str, object] = {}
    for path in candidate_files:
        if not path.exists():
            continue
        try:
            raw = json.loads(path.read_text(encoding="utf-8"))
        except (OSError, ValueError):
            continue
        candidate = raw.get("nodes", raw) if isinstance(raw, dict) else None
        if isinstance(candidate, dict):
            nodes = candidate
            break

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
        merge_node_record(result, normalized, first_seen)
        merge_node_record(result, normalized, last_seen)
    merge_node_history(result, data_dir / "node-history.tsv", now)
    return result

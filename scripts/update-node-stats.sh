#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ENV_FILE="${NODE_STATS_ENV_FILE:-${ROOT_DIR}/../.env}"
DATA_DIR="${NODE_STATS_DATA_DIR:-${ROOT_DIR}/data}"
STATS_FILE="${DATA_DIR}/node-stats.json"
TMP_NODES="${DATA_DIR}/.potatomesh-nodes.tmp"
TMP_STATS="${DATA_DIR}/.node-stats.tmp"

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

if ! command -v curl >/dev/null 2>&1; then
  echo "curl is required." >&2
  exit 1
fi

if ! command -v jq >/dev/null 2>&1; then
  echo "jq is required." >&2
  exit 1
fi

POTATOMESH_API_BASE="${POTATOMESH_API_BASE:-http://127.0.0.1:8083}"
POTATOMESH_NODE_LIMIT="${POTATOMESH_NODE_LIMIT:-5000}"
NOW_EPOCH="$(date -u +%s)"
UPDATED_AT_UTC="$(date -u +"%Y-%m-%dT%H:%M:%SZ")"
NODES_URL="${POTATOMESH_API_BASE%/}/api/nodes?limit=${POTATOMESH_NODE_LIMIT}"

rm -f "${TMP_NODES}" "${TMP_STATS}"

curl -fsS --retry 3 --retry-delay 2 --connect-timeout 10 --max-time 30 \
  "${NODES_URL}" > "${TMP_NODES}"

jq -e 'type == "array"' "${TMP_NODES}" >/dev/null

jq -n \
  --slurpfile nodes "${TMP_NODES}" \
  --argjson now "${NOW_EPOCH}" \
  --arg updated "${UPDATED_AT_UTC}" '
  def heard_at:
    (.last_heard // .lastSeen // .last_seen // 0 | tonumber? // 0);

  ($nodes[0] | map(select((.node_id // .id // "") != ""))) as $heard_nodes
  | {
      totalNodes: ($heard_nodes | length | tostring),
      nodes30Min: ($heard_nodes | map(select(heard_at >= ($now - 1800))) | length | tostring),
      nodes2Hr: ($heard_nodes | map(select(heard_at >= ($now - 7200))) | length | tostring),
      nodes24Hr: ($heard_nodes | map(select(heard_at >= ($now - 86400))) | length | tostring),
      updatedAtUtc: $updated
    }
  ' > "${TMP_STATS}"

mv "${TMP_STATS}" "${STATS_FILE}"
rm -f "${TMP_NODES}"

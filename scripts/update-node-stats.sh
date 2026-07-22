#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ENV_FILE="${NODE_STATS_ENV_FILE:-${ROOT_DIR}/../.env}"
DATA_DIR="${NODE_STATS_DATA_DIR:-${ROOT_DIR}/data}"
STATS_FILE="${DATA_DIR}/node-stats.json"
HISTORY_FILE="${DATA_DIR}/node-history.tsv"
TMP_OBSERVATIONS="${DATA_DIR}/.mqtt-node-observations.tmp"
TMP_HISTORY="${DATA_DIR}/.node-history.tmp"
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

if ! command -v mosquitto_sub >/dev/null 2>&1; then
  echo "mosquitto_sub is required." >&2
  exit 1
fi

if ! command -v jq >/dev/null 2>&1; then
  echo "jq is required." >&2
  exit 1
fi

MQTT_HOST="${MQTT_HOST:-${NODE_STATS_MQTT_HOST:-127.0.0.1}}"
MQTT_PORT="${MQTT_PORT:-${NODE_STATS_MQTT_PORT:-1883}}"
MQTT_TOPIC="${MQTT_TOPIC:-${NODE_STATS_MQTT_TOPIC:-msh/US/#}}"
MQTT_SAMPLE_SECONDS="${MQTT_SAMPLE_SECONDS:-${NODE_STATS_MQTT_SAMPLE_SECONDS:-20}}"
MQTT_NODE_HISTORY_SECONDS="${MQTT_NODE_HISTORY_SECONDS:-${NODE_STATS_MQTT_NODE_HISTORY_SECONDS:-2592000}}"
MQTT_CLIENT_ID="${MQTT_CLIENT_ID:-montanamesh-node-stats}"
NOW_EPOCH="$(date -u +%s)"
UPDATED_AT_UTC="$(date -u +"%Y-%m-%dT%H:%M:%SZ")"
CUTOFF_EPOCH="$((NOW_EPOCH - MQTT_NODE_HISTORY_SECONDS))"

rm -f "${TMP_OBSERVATIONS}" "${TMP_HISTORY}" "${TMP_STATS}"

MOSQUITTO_ARGS=(
  -h "${MQTT_HOST}"
  -p "${MQTT_PORT}"
  -t "${MQTT_TOPIC}"
  -v
  -W "${MQTT_SAMPLE_SECONDS}"
  -i "${MQTT_CLIENT_ID}-$$"
)

if [[ -n "${MQTT_USERNAME:-}" ]]; then
  MOSQUITTO_ARGS+=( -u "${MQTT_USERNAME}" )
fi

if [[ -n "${MQTT_PASSWORD:-}" ]]; then
  MOSQUITTO_ARGS+=( -P "${MQTT_PASSWORD}" )
fi

set +e
mosquitto_sub "${MOSQUITTO_ARGS[@]}" \
  | awk -v now="${NOW_EPOCH}" '
      {
        topic = $1
        while (match(topic, /![[:xdigit:]]{8}/)) {
          node = substr(topic, RSTART + 1, RLENGTH - 1)
          print tolower(node) "\t" now
          topic = substr(topic, RSTART + RLENGTH)
        }
      }
    ' > "${TMP_OBSERVATIONS}"
mosquitto_status="${PIPESTATUS[0]}"
set -e

if [[ "${mosquitto_status}" -ne 0 && "${mosquitto_status}" -ne 27 ]]; then
  echo "mosquitto_sub failed with exit status ${mosquitto_status}." >&2
  exit "${mosquitto_status}"
fi

touch "${HISTORY_FILE}"

{
  awk -F '\t' -v cutoff="${CUTOFF_EPOCH}" '
    NF >= 2 && $1 ~ /^[[:xdigit:]]{8}$/ && $2 ~ /^[0-9]+$/ && $2 >= cutoff {
      print tolower($1) "\t" $2
    }
  ' "${HISTORY_FILE}"
  cat "${TMP_OBSERVATIONS}"
} | awk -F '\t' '
  NF >= 2 && $1 ~ /^[[:xdigit:]]{8}$/ && $2 ~ /^[0-9]+$/ {
    if (!($1 in latest) || $2 > latest[$1]) {
      latest[$1] = $2
    }
  }
  END {
    for (node in latest) {
      print node "\t" latest[node]
    }
  }
' | sort > "${TMP_HISTORY}"

mv "${TMP_HISTORY}" "${HISTORY_FILE}"

jq -n \
  --rawfile history "${HISTORY_FILE}" \
  --argjson now "${NOW_EPOCH}" \
  --arg updated "${UPDATED_AT_UTC}" '
  ($history
    | split("\n")
    | map(select(length > 0))
    | map(split("\t"))
    | map(select(length >= 2 and .[0] != "" and (.[1] | test("^[0-9]+$"))))
    | map({ node: .[0], seenAt: (.[1] | tonumber) })) as $heard_nodes
  | {
      totalNodes: ($heard_nodes | length | tostring),
      nodes30Min: ($heard_nodes | map(select(.seenAt >= ($now - 1800))) | length | tostring),
      nodes2Hr: ($heard_nodes | map(select(.seenAt >= ($now - 7200))) | length | tostring),
      nodes24Hr: ($heard_nodes | map(select(.seenAt >= ($now - 86400))) | length | tostring),
      updatedAtUtc: $updated
    }
  ' > "${TMP_STATS}"

mv "${TMP_STATS}" "${STATS_FILE}"
rm -f "${TMP_OBSERVATIONS}"

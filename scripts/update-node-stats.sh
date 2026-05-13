#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ENV_FILE="${ROOT_DIR}/../.env"
DATA_DIR="${ROOT_DIR}/data"
STATS_FILE="${DATA_DIR}/node-stats.json"
HISTORY_FILE="${DATA_DIR}/node-history.tsv"
TMP_MESSAGES="${DATA_DIR}/.node-messages.tmp"
TMP_UPDATES="${DATA_DIR}/.node-updates.tmp"
TMP_MERGED="${DATA_DIR}/.node-merged.tmp"
TMP_STATS="${DATA_DIR}/.node-stats.tmp"

mkdir -p "${DATA_DIR}"
touch "${HISTORY_FILE}"

if [[ -f "${ENV_FILE}" ]]; then
  # Safely load KEY=VALUE lines from .env without executing shell code.
  while IFS= read -r line || [[ -n "${line}" ]]; do
    [[ -z "${line}" ]] && continue
    [[ "${line}" =~ ^[[:space:]]*# ]] && continue
    [[ "${line}" != *=* ]] && continue

    key="${line%%=*}"
    value="${line#*=}"

    # Trim surrounding whitespace on key.
    key="${key#"${key%%[![:space:]]*}"}"
    key="${key%"${key##*[![:space:]]}"}"

    # Trim surrounding whitespace on value.
    value="${value#"${value%%[![:space:]]*}"}"
    value="${value%"${value##*[![:space:]]}"}"

    # Remove optional surrounding single/double quotes.
    if [[ "${value}" =~ ^\".*\"$ ]]; then
      value="${value:1:${#value}-2}"
    elif [[ "${value}" =~ ^\'.*\'$ ]]; then
      value="${value:1:${#value}-2}"
    fi

    if [[ "${key}" =~ ^[A-Za-z_][A-Za-z0-9_]*$ ]]; then
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

MQTT_PORT="${MQTT_PORT:-1883}"
MQTT_HOST="${MQTT_HOST:-127.0.0.1}"
MQTT_TOPIC="${MQTT_TOPIC:-msh/US/#}"
CAPTURE_SECONDS="${CAPTURE_SECONDS:-75}"
MAX_HISTORY_DAYS="${MAX_HISTORY_DAYS:-90}"
MQTT_USERNAME="${MQTT_USERNAME:-}"
MQTT_PASSWORD="${MQTT_PASSWORD:-}"

NOW_EPOCH="$(date -u +%s)"
PRUNE_BEFORE="$((NOW_EPOCH - (MAX_HISTORY_DAYS * 86400)))"
WINDOW_30_MIN="$((NOW_EPOCH - 1800))"
WINDOW_2_HR="$((NOW_EPOCH - 7200))"
WINDOW_24_HR="$((NOW_EPOCH - 86400))"

MOSQ_ARGS=(-h "${MQTT_HOST}" -p "${MQTT_PORT}" -t "${MQTT_TOPIC}")
if [[ -n "${MQTT_USERNAME}" ]]; then
  MOSQ_ARGS+=(-u "${MQTT_USERNAME}")
fi
if [[ -n "${MQTT_PASSWORD}" ]]; then
  MOSQ_ARGS+=(-P "${MQTT_PASSWORD}")
fi

rm -f "${TMP_MESSAGES}" "${TMP_UPDATES}" "${TMP_MERGED}" "${TMP_STATS}"

set +e
timeout "${CAPTURE_SECONDS}s" mosquitto_sub "${MOSQ_ARGS[@]}" -F '%t' > "${TMP_MESSAGES}" 2>/dev/null
MOSQ_EXIT_CODE=$?
set -e

# timeout exit code (124) is expected when capture window ends.
# non-timeout failures likely indicate auth/connect issues; don't overwrite stats with zeros.
if [[ "${MOSQ_EXIT_CODE}" -ne 0 && "${MOSQ_EXIT_CODE}" -ne 124 && ! -s "${TMP_MESSAGES}" ]]; then
  echo "mosquitto_sub failed (exit ${MOSQ_EXIT_CODE}). Check MQTT host/port/username/password." >&2
  rm -f "${TMP_MESSAGES}" "${TMP_UPDATES}" "${TMP_MERGED}" "${TMP_STATS}"
  exit 1
fi

if [[ -s "${TMP_MESSAGES}" ]]; then
  awk -F'/' -v now="${NOW_EPOCH}" '
    {
      node = ""
      for (i = 1; i <= NF; i++) {
        if ($i ~ /^![[:alnum:]]+$/) {
          node = $i
          sub(/^!/, "", node)
          break
        }
      }
      if (node != "") {
        print node "\t" now
      }
    }
  ' "${TMP_MESSAGES}" > "${TMP_UPDATES}" || true
fi

if [[ -s "${TMP_UPDATES}" ]]; then
  cat "${HISTORY_FILE}" "${TMP_UPDATES}" \
    | awk -F'\t' -v prune_before="${PRUNE_BEFORE}" '
      NF >= 2 {
        node = $1
        ts = $2 + 0
        if (ts >= prune_before && ts > seen[node]) {
          seen[node] = ts
        }
      }
      END {
        for (node in seen) {
          print node "\t" seen[node]
        }
      }
    ' > "${TMP_MERGED}"
else
  awk -F'\t' -v prune_before="${PRUNE_BEFORE}" '
    NF >= 2 {
      node = $1
      ts = $2 + 0
      if (ts >= prune_before) {
        print node "\t" ts
      }
    }
  ' "${HISTORY_FILE}" > "${TMP_MERGED}"
fi

mv "${TMP_MERGED}" "${HISTORY_FILE}"

TOTAL_NODES="$(awk 'NF >= 2 { count++ } END { print count + 0 }' "${HISTORY_FILE}")"
NODES_30_MIN="$(awk -F'\t' -v threshold="${WINDOW_30_MIN}" 'NF >= 2 && ($2 + 0) >= threshold { count++ } END { print count + 0 }' "${HISTORY_FILE}")"
NODES_2_HR="$(awk -F'\t' -v threshold="${WINDOW_2_HR}" 'NF >= 2 && ($2 + 0) >= threshold { count++ } END { print count + 0 }' "${HISTORY_FILE}")"
NODES_24_HR="$(awk -F'\t' -v threshold="${WINDOW_24_HR}" 'NF >= 2 && ($2 + 0) >= threshold { count++ } END { print count + 0 }' "${HISTORY_FILE}")"
UPDATED_AT_UTC="$(date -u +"%Y-%m-%dT%H:%M:%SZ")"

jq -n \
  --arg total "${TOTAL_NODES}" \
  --arg n30 "${NODES_30_MIN}" \
  --arg n2 "${NODES_2_HR}" \
  --arg n24 "${NODES_24_HR}" \
  --arg updated "${UPDATED_AT_UTC}" \
  '{
    totalNodes: $total,
    nodes30Min: $n30,
    nodes2Hr: $n2,
    nodes24Hr: $n24,
    updatedAtUtc: $updated
  }' > "${TMP_STATS}"

mv "${TMP_STATS}" "${STATS_FILE}"
rm -f "${TMP_MESSAGES}" "${TMP_UPDATES}"

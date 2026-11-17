(() => {
  const status = document.getElementById("nodeStatsStatus");
  const fields = document.querySelectorAll("[data-node-stat]");
  const sourceNames = {
    "meshmonitor-db": "MeshMonitor",
    mqtt: "MQTT traffic"
  };
  const staleAfter = 15 * 60 * 1000;

  async function loadNodeStats() {
    try {
      status.textContent = "Loading node statistics…";
      const response = await fetch("/api/nodes/stats", { cache: "no-store" });
      if (!response.ok) {
        throw new Error(`HTTP ${response.status}`);
      }

      const snapshot = await response.json();
      const updated = Date.parse(snapshot.updatedAtUtc);
      if (!Number.isFinite(updated)) {
        status.textContent = "Node statistics are unavailable.";
        return;
      }

      fields.forEach(field => {
        const value = snapshot[field.dataset.nodeStat];
        field.textContent = /^\d+$/.test(value) ? value : "--";
      });

      const time = new Intl.DateTimeFormat(undefined, {
        month: "short", day: "numeric", hour: "numeric", minute: "2-digit"
      }).format(updated);
      const source = Object.hasOwn(sourceNames, snapshot.source) ? ` from ${sourceNames[snapshot.source]}` : "";
      const stale = Date.now() - updated > staleAfter;
      status.textContent = stale ? `Counts are stale. Last updated ${time}${source}.` : `Updated ${time}${source}.`;
    } catch (error) {
      console.error("Unable to load node stats", error);
      status.textContent = "Node statistics are unavailable.";
    }
  }

  if (status) {
    loadNodeStats();
  }
})();

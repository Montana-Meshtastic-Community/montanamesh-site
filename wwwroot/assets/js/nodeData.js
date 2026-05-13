async function loadNodeStats() {
  try {
    const response = await fetch(`/api/nodes/stats?t=${Date.now()}`, { cache: "no-store" });
    if (!response.ok) {
      throw new Error(`HTTP ${response.status}`);
    }

    const data = await response.json();
    document.getElementById("totalNodes").textContent = data.totalNodes;
    document.getElementById("nodes30min").textContent = data.nodes30Min;
    document.getElementById("nodes2hr").textContent = data.nodes2Hr;
    document.getElementById("nodes24hr").textContent = data.nodes24Hr;
  } catch (error) {
    console.error("Unable to load node stats", error);
  }
}

loadNodeStats();

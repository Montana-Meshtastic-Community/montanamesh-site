(() => {
  function setupTheme() {
    const key = "montana_theme";
    const root = document.documentElement;
    const toggle = document.getElementById("themeToggle");
    const toggleIcon = document.getElementById("themeToggleIcon");

    function applyTheme(theme) {
      const dark = theme === "dark";
      root.dataset.theme = theme;
      if (toggleIcon) {
        toggleIcon.textContent = dark ? "☀" : "☾";
      }
      const switchLabel = dark ? "Switch to light theme" : "Switch to dark theme";
      toggle?.setAttribute("aria-label", switchLabel);
      toggle?.setAttribute("title", switchLabel);
    }

    let savedTheme;
    try {
      savedTheme = localStorage.getItem(key);
    } catch {
      // Browsers may deny storage access for this origin.
    }
    applyTheme(savedTheme === "light" ? "light" : "dark");

    toggle?.addEventListener("click", () => {
      const nextTheme = root.dataset.theme === "dark" ? "light" : "dark";
      applyTheme(nextTheme);
      try {
        localStorage.setItem(key, nextTheme);
      } catch {
        // Theme changes still work for this page when storage is unavailable.
      }
    });
  }

  async function loadRepoStats() {
    const endpoint = document.getElementById("repoStatsLink")?.dataset.repoApi;
    const stars = document.getElementById("repoStars");
    const forks = document.getElementById("repoForks");
    if (!endpoint || !stars || !forks) {
      return;
    }

    try {
      const response = await fetch(endpoint, {
        headers: { Accept: "application/vnd.github+json" }
      });
      if (response.ok) {
        const repo = await response.json();
        stars.textContent = String(repo.stargazers_count ?? "-");
        forks.textContent = String(repo.forks_count ?? "-");
      }
    } catch {
      // Keep the displayed dashes if GitHub is unreachable.
    }
  }

  function setupReveals() {
    const elements = document.querySelectorAll(".reveal");
    if (!elements.length || !("IntersectionObserver" in window) || window.matchMedia("(prefers-reduced-motion: reduce)").matches) {
      return;
    }

    const observer = new IntersectionObserver(entries => {
      for (const entry of entries) {
        if (entry.isIntersecting) {
          entry.target.classList.add("is-visible");
          observer.unobserve(entry.target);
        }
      }
    }, { threshold: 0.12 });

    elements.forEach((element, index) => {
      element.style.transitionDelay = `${Math.min(index * 70, 280)}ms`;
      observer.observe(element);
    });
    document.body.classList.add("reveal-ready");
  }

  setupTheme();
  loadRepoStats();
  setupReveals();
})();

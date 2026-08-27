(() => {
  const key = "montana_theme";
  const root = document.documentElement;
  const toggle = document.getElementById("themeToggle");
  const toggleIcon = document.getElementById("themeToggleIcon");
  const starsEl = document.getElementById("repoStars");
  const forksEl = document.getElementById("repoForks");

  function applyTheme(theme) {
    root.setAttribute("data-theme", theme);
    if (toggleIcon) {
      toggleIcon.textContent = theme === "dark" ? "L" : "D";
    }
    if (toggle) {
      toggle.setAttribute("aria-label", theme === "dark" ? "Switch to light theme" : "Switch to dark theme");
      toggle.setAttribute("title", theme === "dark" ? "Switch to light theme" : "Switch to dark theme");
    }
  }

  const savedTheme = localStorage.getItem(key);
  const initialTheme = savedTheme === "light" || savedTheme === "dark" ? savedTheme : "dark";
  applyTheme(initialTheme);

  if (toggle) {
    toggle.addEventListener("click", () => {
      const nextTheme = root.getAttribute("data-theme") === "dark" ? "light" : "dark";
      localStorage.setItem(key, nextTheme);
      applyTheme(nextTheme);
    });
  }

  async function loadRepoStats() {
    if (!starsEl || !forksEl) {
      return;
    }

    try {
      const response = await fetch("https://api.github.com/repos/Montana-Meshtastic-Community/montanamesh-site", {
        headers: {
          Accept: "application/vnd.github+json"
        }
      });

      if (!response.ok) {
        return;
      }

      const repo = await response.json();
      starsEl.textContent = String(repo.stargazers_count ?? "-");
      forksEl.textContent = String(repo.forks_count ?? "-");
    } catch {
      // Keep placeholders if request fails.
    }
  }

  loadRepoStats();

  const revealEls = document.querySelectorAll(".reveal");
  if (revealEls.length && !window.matchMedia("(prefers-reduced-motion: reduce)").matches) {
    document.body.classList.add("reveal-ready");
    const observer = new IntersectionObserver((entries) => {
      entries.forEach((entry) => {
        if (entry.isIntersecting) {
          entry.target.classList.add("is-visible");
          observer.unobserve(entry.target);
        }
      });
    }, { threshold: 0.12 });

    revealEls.forEach((el, index) => {
      el.style.transitionDelay = `${Math.min(index * 70, 280)}ms`;
      observer.observe(el);
    });
  } else {
    revealEls.forEach((el) => el.classList.add("is-visible"));
  }
})();

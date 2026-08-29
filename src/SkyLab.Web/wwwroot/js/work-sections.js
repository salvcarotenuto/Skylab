document.addEventListener("DOMContentLoaded", () => {
  const tabs = Array.from(document.querySelectorAll("[data-work-tab]"));
  const panels = Array.from(document.querySelectorAll("[data-work-tab-panel]"));
  if (!tabs.length || !panels.length) return;
  const show = (name, updateHash = true) => {
    tabs.forEach((tab) => tab.classList.toggle("is-active", tab.dataset.workTab === name));
    panels.forEach((panel) => { panel.hidden = panel.dataset.workTabPanel !== name; });
    if (updateHash) history.replaceState(null, "", `#${name}`);
  };
  tabs.forEach((tab) => tab.addEventListener("click", () => show(tab.dataset.workTab)));
  const requested = location.hash.slice(1);
  show(tabs.some((tab) => tab.dataset.workTab === requested) ? requested : "pianificazione", false);
});

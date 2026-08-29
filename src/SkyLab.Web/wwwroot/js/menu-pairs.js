(() => {
  const sections = [...document.querySelectorAll(".main-menu > .menu-section")];
  if (sections.length < 2) return;

  const storageKey = "skylab.mainMenu.openSections";
  const isTwoColumns = () => window.matchMedia("(min-width: 761px)").matches;
  const saveState = () => {
    const openIds = sections.filter(section => section.open).map(section => section.id);
    sessionStorage.setItem(storageKey, JSON.stringify(openIds));
  };

  try {
    const openIds = JSON.parse(sessionStorage.getItem(storageKey) || "[]");
    if (Array.isArray(openIds)) {
      sections.forEach(section => { section.open = openIds.includes(section.id); });
    }
  } catch {
    sessionStorage.removeItem(storageKey);
  }

  let syncing = false;

  sections.forEach((section, index) => {
    section.addEventListener("toggle", () => {
      if (syncing) return;
      if (!isTwoColumns()) {
        saveState();
        return;
      }

      const peerIndex = index % 2 === 0 ? index + 1 : index - 1;
      const peer = sections[peerIndex];
      if (!peer || peer.open === section.open) {
        saveState();
        return;
      }

      syncing = true;
      peer.open = section.open;
      requestAnimationFrame(() => {
        syncing = false;
        saveState();
      });
    });
  });

  document.addEventListener("keydown", event => {
    if (event.key !== "Escape" || !sections.some(section => section.open)) return;

    event.preventDefault();
    syncing = true;
    sections.forEach(section => { section.open = false; });
    sessionStorage.removeItem(storageKey);
    requestAnimationFrame(() => { syncing = false; });
  });
})();

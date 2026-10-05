(() => {
  const sections = [...document.querySelectorAll(".main-menu > .menu-section")];
  if (sections.length < 2) return;

  const storageKey = "skylab.mainMenu.openSections";
  const isTwoColumns = () => window.matchMedia("(min-width: 761px)").matches;
  const revealMargins = 16;
  const saveState = () => {
    const openIds = sections.filter(section => section.open).map(section => section.id);
    sessionStorage.setItem(storageKey, JSON.stringify(openIds));
  };
  const revealSection = section => {
    requestAnimationFrame(() => requestAnimationFrame(() => {
      if (!section.open) return;

      const bounds = section.getBoundingClientRect();
      const availableHeight = window.innerHeight - revealMargins * 2;
      let scrollAdjustment = 0;

      if (bounds.height > availableHeight || bounds.top < revealMargins) {
        scrollAdjustment = bounds.top - revealMargins;
      } else if (bounds.bottom > window.innerHeight - revealMargins) {
        scrollAdjustment = bounds.bottom - window.innerHeight + revealMargins;
      }

      if (Math.abs(scrollAdjustment) > 1) {
        window.scrollBy({ top: scrollAdjustment, behavior: "smooth" });
      }
    }));
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
        if (section.open) revealSection(section);
        return;
      }

      const peerIndex = index % 2 === 0 ? index + 1 : index - 1;
      const peer = sections[peerIndex];
      if (!peer || peer.open === section.open) {
        saveState();
        if (section.open) revealSection(section);
        return;
      }

      syncing = true;
      peer.open = section.open;
      requestAnimationFrame(() => {
        syncing = false;
        saveState();
        if (section.open) revealSection(section);
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

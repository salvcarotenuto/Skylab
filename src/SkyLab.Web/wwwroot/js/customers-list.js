document.addEventListener("DOMContentLoaded", () => {
  const page = document.querySelector("[data-customer-list-page]");
  const search = page?.querySelector("[data-customer-search]");
  const inactive = page?.querySelector("[data-customer-inactive]");
  const form = page?.querySelector("[data-customer-search-form]");
  const rows = Array.from(page?.querySelectorAll("[data-customer-row]") ?? []);
  const listFrame = page?.querySelector(".customer-list-frame");
  const count = page?.querySelector("[data-customer-count]");
  const empty = page?.querySelector("[data-customer-empty]");
  const exitLink = page?.querySelector("[data-customer-exit]");
  let selected = null;

  if (!page || !search) return;

  const normalize = (value) => String(value ?? "")
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .toLocaleLowerCase("it-IT")
    .trim();

  const visibleRows = () => rows.filter((row) => !row.hidden);

  const ensureVisible = (row, direction = 0) => {
    if (!row || !listFrame) return;
    const frameRect = listFrame.getBoundingClientRect();
    const rowRect = row.getBoundingClientRect();
    if (rowRect.top >= frameRect.top && rowRect.bottom <= frameRect.bottom) return;
    if (direction >= 0 && rowRect.bottom > frameRect.bottom) listFrame.scrollTop += rowRect.bottom - frameRect.bottom;
    if (direction <= 0 && rowRect.top < frameRect.top) listFrame.scrollTop -= frameRect.top - rowRect.top;
  };

  const choose = (row, focus = false, direction = 0) => {
    rows.forEach((item) => item.classList.toggle("is-selected", item === row));
    selected = row;
    if (row && focus) row.focus({ preventScroll: true });
    ensureVisible(row, direction);
  };

  const moveSelection = (key, source = null) => {
    const visible = visibleRows();
    if (!visible.length) return;
    let index = source ? visible.indexOf(source) : visible.indexOf(selected);
    if (index < 0) index = 0;
    if (key === "Home") return choose(visible[0], true, -1);
    if (key === "End") return choose(visible.at(-1), true, 1);
    const direction = key === "ArrowDown" ? 1 : -1;
    index = Math.max(0, Math.min(visible.length - 1, index + direction));
    choose(visible[index], true, direction);
  };

  const openSelected = () => selected?.querySelector("[data-customer-primary]")?.click();

  const applyFilter = () => {
    const query = normalize(search.value);
    let visible = 0;

    rows.forEach((row) => {
      const name = normalize(row.dataset.filterName);
      const code = normalize(row.dataset.filterCode);
      const matches = query === "" || name.includes(query) || code.includes(query);
      row.hidden = !matches;
      if (matches) visible += 1;
    });

    if (count) count.textContent = String(visible);
    if (empty) empty.hidden = visible !== 0;
    if (!selected || selected.hidden) choose(visibleRows()[0] ?? null);
  };

  search.addEventListener("input", applyFilter);
  search.addEventListener("keydown", (event) => {
    if (["ArrowDown", "ArrowUp", "Home", "End"].includes(event.key)) {
      event.preventDefault();
      moveSelection(event.key);
    } else if (event.key === "Enter") {
      event.preventDefault();
      openSelected();
    }
  });
  inactive?.addEventListener("change", () => form?.requestSubmit());

  rows.forEach((row) => {
    row.addEventListener("click", () => choose(row));
    row.addEventListener("keydown", (event) => {
      if (event.target !== row) return;
      if (["ArrowDown", "ArrowUp", "Home", "End"].includes(event.key)) {
        event.preventDefault();
        moveSelection(event.key, row);
      } else if (event.key === "Enter") {
        event.preventDefault();
        openSelected();
      }
    });
  });

  let lastScroll = listFrame?.scrollTop ?? 0;
  let scrollFrame = 0;
  listFrame?.addEventListener("scroll", () => {
    if (scrollFrame) cancelAnimationFrame(scrollFrame);
    scrollFrame = requestAnimationFrame(() => {
      scrollFrame = 0;
      const delta = listFrame.scrollTop - lastScroll;
      lastScroll = listFrame.scrollTop;
      if (!delta || !selected) return;
      const frameRect = listFrame.getBoundingClientRect();
      const selectedRect = selected.getBoundingClientRect();
      if (selectedRect.bottom > frameRect.top && selectedRect.top < frameRect.bottom) return;
      const inView = visibleRows().filter((row) => {
        const rowRect = row.getBoundingClientRect();
        return rowRect.top >= frameRect.top && rowRect.bottom <= frameRect.bottom;
      });
      if (inView.length) choose(delta > 0 ? inView[0] : inView.at(-1));
    });
  }, { passive: true });

  document.addEventListener("keydown", (event) => {
    if (event.key !== "Escape") return;
    event.preventDefault();
    event.stopPropagation();
    location.href = exitLink?.href ?? "/";
  }, true);

  document.addEventListener("keydown", (event) => {
    const target = event.target;
    const isEditable = target instanceof HTMLInputElement ||
      target instanceof HTMLTextAreaElement ||
      target instanceof HTMLSelectElement ||
      target instanceof HTMLButtonElement ||
      target?.isContentEditable;

    if (event.defaultPrevented || isEditable || event.altKey || event.ctrlKey || event.metaKey) return;

    if (["ArrowDown", "ArrowUp", "Home", "End"].includes(event.key)) {
      event.preventDefault();
      moveSelection(event.key);
      return;
    }

    if (event.key === "Enter") {
      event.preventDefault();
      openSelected();
      return;
    }

    if (event.key.length === 1) {
      event.preventDefault();
      search.value += event.key;
      search.focus({ preventScroll: true });
      applyFilter();
      return;
    }

    if (event.key === "Backspace" && search.value !== "") {
      event.preventDefault();
      search.value = search.value.slice(0, -1);
      search.focus({ preventScroll: true });
      applyFilter();
    }
  });

  applyFilter();
});

document.addEventListener("DOMContentLoaded", () => {
  const page = document.querySelector("[data-list-page]");
  if (!page) return;

  const rows = [...page.querySelectorAll("[data-user-row]")];
  const grid = page.querySelector("[data-user-grid]");
  const gridHead = grid?.querySelector("thead");
  const search = page.querySelector("[data-user-search]");
  const clear = page.querySelector("[data-user-search-clear]");
  const count = page.querySelector("[data-user-count]");
  const empty = page.querySelector("[data-user-empty]");
  let selected = null;
  let lastScrollTop = grid?.scrollTop || 0;

  const visibleRows = () => rows.filter(row => !row.hidden);
  const keepVisible = row => {
    if (!row || !grid) return;
    const frame = grid.getBoundingClientRect();
    const rect = row.getBoundingClientRect();
    const top = frame.top + (gridHead?.getBoundingClientRect().height || 0);
    if (rect.top < top) grid.scrollTop -= top - rect.top;
    else if (rect.bottom > frame.bottom) grid.scrollTop += rect.bottom - frame.bottom;
  };
  const select = (row, focus = false) => {
    rows.forEach(item => {
      item.classList.remove("selected-row");
      item.removeAttribute("aria-selected");
    });
    selected = row;
    if (!row) return;
    row.classList.add("selected-row");
    row.setAttribute("aria-selected", "true");
    if (focus) row.focus({ preventScroll: true });
    keepVisible(row);
  };
  const filter = () => {
    const query = (search?.value || "").trim().toLocaleLowerCase("it");
    let visibleCount = 0;
    rows.forEach(row => {
      row.hidden = query !== "" && !row.dataset.filterName.toLocaleLowerCase("it").includes(query);
      if (!row.hidden) visibleCount++;
    });
    if (count) count.textContent = String(visibleCount);
    if (empty) empty.hidden = visibleCount > 0;
    if (clear) clear.hidden = !search?.value;
    if (!selected || selected.hidden) select(visibleRows()[0] || null);
  };
  const openSelected = () => {
    if (selected) location.href = selected.dataset.editUrl;
  };

  rows.forEach(row => {
    row.addEventListener("click", () => select(row, true));
    row.addEventListener("dblclick", openSelected);
    row.addEventListener("keydown", event => {
      const visible = visibleRows();
      const index = Math.max(0, visible.indexOf(row));
      let next = null;
      if (event.key === "Enter") {
        event.preventDefault();
        openSelected();
      } else if (event.key === "ArrowDown") next = visible[Math.min(index + 1, visible.length - 1)];
      else if (event.key === "ArrowUp") next = visible[Math.max(index - 1, 0)];
      else if (event.key === "Home") next = visible[0];
      else if (event.key === "End") next = visible.at(-1);
      if (next) {
        event.preventDefault();
        select(next, true);
      }
    });
  });

  page.querySelector('[data-user-action="add"]')?.addEventListener("click", () => {
    location.href = page.dataset.addUrl;
  });
  page.querySelector('[data-user-action="edit"]')?.addEventListener("click", openSelected);
  page.querySelector('[data-user-action="exit"]')?.addEventListener("click", () => {
    location.href = page.dataset.menuUrl || "/";
  });
  page.querySelector('[data-user-action="delete"]')?.addEventListener("click", () => {
    if (!selected) return;
    window.SkyLabMessageBox?.show({
      mode: "confirm",
      variant: "confirm",
      title: "Elimina utente",
      message: `Eliminare l'utente ${selected.dataset.recordLabel}?`,
      detail: "L'operazione non potrà essere annullata.",
      okText: "Elimina",
      onConfirm: () => {
        const form = page.querySelector("[data-user-delete-form]");
        form.querySelector("[data-user-delete-code]").value = selected.dataset.deleteCode;
        form.submit();
      }
    });
  });

  search?.addEventListener("input", filter);
  search?.addEventListener("keydown", event => {
    if (event.key !== "ArrowDown" && event.key !== "ArrowUp") return;
    const visible = visibleRows();
    if (!visible.length) return;
    event.preventDefault();
    select(event.key === "ArrowDown" ? visible[0] : visible.at(-1), true);
  });
  clear?.addEventListener("click", () => {
    search.value = "";
    filter();
    search.focus();
  });

  grid?.addEventListener("scroll", () => {
    const direction = grid.scrollTop >= lastScrollTop ? 1 : -1;
    lastScrollTop = grid.scrollTop;
    if (!selected || selected.hidden) return;
    const frame = grid.getBoundingClientRect();
    const rect = selected.getBoundingClientRect();
    const top = frame.top + (gridHead?.getBoundingClientRect().height || 0);
    if (rect.top >= top && rect.bottom <= frame.bottom) return;
    const inView = visibleRows().filter(row => {
      const item = row.getBoundingClientRect();
      return item.top >= top && item.bottom <= frame.bottom;
    });
    if (inView.length) select(direction > 0 ? inView[0] : inView.at(-1));
  }, { passive: true });

  document.addEventListener("keydown", event => {
    if (event.defaultPrevented || event.altKey || event.ctrlKey || event.metaKey) return;
    if (event.key === "Escape") {
      if (document.querySelector("dialog[open]")) return;
      event.preventDefault();
      location.href = page.dataset.menuUrl || "/";
      return;
    }
    const target = event.target;
    const isEditable = target instanceof HTMLInputElement
      || target instanceof HTMLSelectElement
      || target instanceof HTMLTextAreaElement
      || target instanceof HTMLButtonElement
      || target?.isContentEditable;
    if (isEditable) return;
    if (event.key.length === 1) {
      event.preventDefault();
      search.value += event.key;
      search.focus();
      filter();
    } else if (event.key === "Backspace" && search.value) {
      event.preventDefault();
      search.value = search.value.slice(0, -1);
      search.focus();
      filter();
    }
  });

  document.querySelectorAll("[data-page-message]").forEach(item => {
    window.SkyLabMessageBox?.show({ message: item.dataset.messageText, variant: "error" });
  });
  filter();
  select(visibleRows()[0] || null);
});

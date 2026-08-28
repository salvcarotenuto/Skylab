document.addEventListener("DOMContentLoaded", () => {
  const code = document.querySelector("[data-machine-article-code]");
  const description = document.querySelector("[data-machine-article-description]");
  const category = document.querySelector("[name='Macchina.CategoryId']");
  const duration = document.querySelector("[name='Macchina.DurationDays']");
  const suppliedQuantity = document.querySelector("[data-machine-supplied-quantity]");
  const dailyConsumption = document.querySelector("[data-machine-daily-consumption]");
  const installedDate = document.querySelector("[name='Macchina.InstalledOn'][data-filter-date-hidden]");
  const nextDate = document.querySelector("[name='Macchina.NextServiceOn'][data-filter-date-hidden]");
  const nextDateDisplay = nextDate?.closest("[data-date-control]")?.querySelector("[data-filter-date-display]");
  const lookup = document.querySelector("[data-machine-article-lookup]");
  if (!code || !description || !lookup) return;

  const frame = lookup.querySelector("[data-machine-article-lookup-frame]");
  const search = lookup.querySelector("[data-machine-article-search]");
  const count = lookup.querySelector("[data-machine-article-count]");
  let allRows = Array.from(lookup.querySelectorAll("[data-machine-article-row]"));
  let visibleRows = allRows;
  let selectedIndex = -1;
  let sortKey = "";
  let sortDirection = "desc";
  let nextDateWasEdited = false;

  const parseIsoDate = (value) => {
    const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value || "");
    return match ? new Date(Number(match[1]), Number(match[2]) - 1, Number(match[3])) : null;
  };
  const pad = (value) => String(value).padStart(2, "0");
  const proposeNextDate = (force = false) => {
    if (nextDateWasEdited && !force) return;
    const base = parseIsoDate(installedDate?.value);
    if (!base || !nextDate || !nextDateDisplay) return;
    const isConsumable = Number(category?.value || 0) === 3;
    const quantity = window.MicronoteDecimal?.parse(suppliedQuantity?.value) || 0;
    const consumption = window.MicronoteDecimal?.parse(dailyConsumption?.value) || 0;
    const cycleDays = Number.parseInt(duration?.value || "0", 10) || 0;
    const days = isConsumable && quantity > 0 && consumption > 0 ? Math.ceil(quantity / consumption) : cycleDays;
    if (days <= 0) return;
    const proposed = new Date(base.getFullYear(), base.getMonth(), base.getDate() + days);
    nextDate.value = `${proposed.getFullYear()}-${pad(proposed.getMonth() + 1)}-${pad(proposed.getDate())}`;
    nextDateDisplay.value = `${pad(proposed.getDate())}/${pad(proposed.getMonth() + 1)}/${proposed.getFullYear()}`;
    nextDate.dispatchEvent(new Event("change", { bubbles: true }));
  };

  const applyArticle = (row) => {
    if (!row) return;
    code.value = row.dataset.code || "";
    description.value = row.dataset.description || "";
    if (category) category.value = row.dataset.categoryCode || "";
    if (duration) duration.value = row.dataset.duration || "0";
    if (dailyConsumption) dailyConsumption.value = window.MicronoteDecimal?.format(row.dataset.consumption, 3) || "";
    nextDateWasEdited = false;
    proposeNextDate(true);
    code.dispatchEvent(new Event("change", { bubbles: true }));
  };

  const selectedRow = () => visibleRows[selectedIndex] || null;
  const setSelected = (index, focusRow = false) => {
    if (!visibleRows.length) {
      selectedIndex = -1;
      allRows.forEach(row => { row.classList.remove("selected"); row.setAttribute("aria-selected", "false"); });
      return;
    }
    selectedIndex = Math.max(0, Math.min(index, visibleRows.length - 1));
    allRows.forEach(row => { row.classList.remove("selected"); row.setAttribute("aria-selected", "false"); });
    const row = visibleRows[selectedIndex];
    row.classList.add("selected");
    row.setAttribute("aria-selected", "true");
    row.scrollIntoView({ block: "nearest" });
    if (focusRow) row.focus({ preventScroll: true });
  };

  const filterRows = () => {
    const previous = selectedRow();
    const text = (search.value || "").trim().toLocaleLowerCase("it");
    visibleRows = allRows.filter(row => {
      const visible = !text || (row.dataset.code || "").toLocaleLowerCase("it").includes(text)
        || (row.dataset.description || "").toLocaleLowerCase("it").includes(text)
        || (row.dataset.category || "").toLocaleLowerCase("it").includes(text);
      row.hidden = !visible;
      return visible;
    });
    count.value = String(visibleRows.length);
    const previousIndex = previous ? visibleRows.indexOf(previous) : -1;
    setSelected(previousIndex >= 0 ? previousIndex : 0);
  };

  const sortRows = (key) => {
    sortDirection = sortKey === key && sortDirection === "asc" ? "desc" : "asc";
    sortKey = key;
    const direction = sortDirection === "asc" ? 1 : -1;
    allRows.sort((left, right) => {
      if (key === "price") return (Number(left.dataset.price || 0) - Number(right.dataset.price || 0)) * direction;
      return (left.dataset[key] || "").localeCompare(right.dataset[key] || "", "it", {
        sensitivity: "base",
        numeric: true
      }) * direction;
    });
    const body = lookup.querySelector("tbody");
    allRows.forEach(row => body.appendChild(row));
    lookup.querySelectorAll("[data-machine-article-sort]").forEach(button => {
      const active = button.dataset.machineArticleSort === sortKey;
      button.classList.toggle("is-active", active);
      button.dataset.direction = active ? sortDirection : "";
    });
    filterRows();
  };

  const closeLookup = () => {
    lookup.hidden = true;
    document.body.classList.remove("lookup-open");
    code.focus();
  };
  const openLookup = () => {
    lookup.hidden = false;
    document.body.classList.add("lookup-open");
    search.value = "";
    filterRows();
    const current = visibleRows.findIndex(row => (row.dataset.code || "").localeCompare(code.value.trim(), "it", { sensitivity: "base" }) === 0);
    if (current >= 0) setSelected(current);
    search.focus();
  };
  const confirmLookup = () => {
    const row = visibleRows[selectedIndex];
    if (!row) return;
    applyArticle(row);
    closeLookup();
  };
  const findTypedArticle = () => {
    const typed = code.value.trim();
    const row = allRows.find(candidate => (candidate.dataset.code || "").localeCompare(typed, "it", { sensitivity: "base" }) === 0);
    description.value = row?.dataset.description || "";
  };

  const gridViewport = () => {
    const gridRect = frame.getBoundingClientRect();
    const headerHeight = lookup.querySelector("thead")?.getBoundingClientRect().height ?? 0;
    return { top: gridRect.top + headerHeight, bottom: gridRect.bottom };
  };
  const isRowVisible = (row, viewport) => {
    const rowRect = row.getBoundingClientRect();
    return rowRect.bottom > viewport.top + 1 && rowRect.top < viewport.bottom - 1;
  };
  const fullyVisibleRows = (viewport) => visibleRows.filter(row => {
    const rowRect = row.getBoundingClientRect();
    return rowRect.top >= viewport.top + 1 && rowRect.bottom <= viewport.bottom - 1;
  });
  const selectVisibleRowFromScroll = (delta) => {
    if (delta === 0) return;
    const viewport = gridViewport();
    const selected = selectedRow();
    if (!selected || isRowVisible(selected, viewport)) return;
    const currentRows = fullyVisibleRows(viewport);
    if (!currentRows.length) return;
    setSelected(visibleRows.indexOf(delta > 0 ? currentRows[0] : currentRows[currentRows.length - 1]));
  };

  let lastScrollTop = frame.scrollTop;
  let scrollFrame = 0;
  frame.addEventListener("scroll", () => {
    if (scrollFrame) window.cancelAnimationFrame(scrollFrame);
    scrollFrame = window.requestAnimationFrame(() => {
      scrollFrame = 0;
      const currentScrollTop = frame.scrollTop;
      const delta = currentScrollTop - lastScrollTop;
      lastScrollTop = currentScrollTop;
      selectVisibleRowFromScroll(delta);
    });
  }, { passive: true });

  let wheelFrame = 0;
  frame.addEventListener("wheel", (event) => {
    if (wheelFrame) window.cancelAnimationFrame(wheelFrame);
    const delta = event.deltaY;
    wheelFrame = window.requestAnimationFrame(() => {
      wheelFrame = window.requestAnimationFrame(() => {
        wheelFrame = 0;
        selectVisibleRowFromScroll(delta);
      });
    });
  }, { passive: true });

  document.querySelector("[data-machine-article-lookup-open]")?.addEventListener("click", openLookup);
  lookup.querySelector("[data-machine-article-ok]")?.addEventListener("click", confirmLookup);
  lookup.querySelector("[data-machine-article-cancel]")?.addEventListener("click", closeLookup);
  search.addEventListener("input", filterRows);
  const navigate = (event) => {
    if (event.target.closest("button") && event.key !== "Escape") return;
    if (event.key === "ArrowDown") { event.preventDefault(); setSelected(selectedIndex + 1); }
    else if (event.key === "ArrowUp") { event.preventDefault(); setSelected(selectedIndex - 1); }
    else if (event.key === "Enter") { event.preventDefault(); confirmLookup(); }
    else if (event.key === "Escape") { event.preventDefault(); closeLookup(); }
  };
  lookup.addEventListener("keydown", navigate);
  allRows.forEach(row => {
    row.addEventListener("click", () => setSelected(visibleRows.indexOf(row), true));
    row.addEventListener("dblclick", confirmLookup);
  });
  code.addEventListener("change", findTypedArticle);
  code.addEventListener("blur", findTypedArticle);
  installedDate?.addEventListener("change", () => proposeNextDate());
  category?.addEventListener("change", () => proposeNextDate());
  duration?.addEventListener("change", () => proposeNextDate());
  suppliedQuantity?.addEventListener("change", () => proposeNextDate());
  dailyConsumption?.addEventListener("change", () => proposeNextDate());
  nextDateDisplay?.addEventListener("input", () => { nextDateWasEdited = true; });
  lookup.querySelectorAll("[data-machine-article-sort]").forEach(button => {
    button.addEventListener("click", () => sortRows(button.dataset.machineArticleSort));
  });
});

document.addEventListener("DOMContentLoaded", () => {
  const page = document.querySelector("[data-agenda-list-page]");
  const grid = page?.querySelector("[data-agenda-grid]");
  const rows = [...(grid?.querySelectorAll("[data-agenda-row]") ?? [])];
  const period = page?.querySelector("[data-agenda-period]");
  const summary = page?.querySelector("[data-agenda-period-summary]");
  const search = page?.querySelector("[data-agenda-search]");
  const status = page?.querySelector("[data-agenda-status]");
  const clear = page?.querySelector("[data-agenda-search-clear]");
  const count = page?.querySelector("[data-agenda-count]");
  const empty = page?.querySelector("[data-agenda-empty]");
  const checkAll = page?.querySelector("[data-agenda-check-all]");
  const selectedCount = page?.querySelector("[data-agenda-selected-count]");
  const printButtons = [...(page?.querySelectorAll("[data-agenda-print]") ?? [])];
  const periodDialog = document.querySelector("[data-agenda-period-dialog]");
  const customFrom = periodDialog?.querySelector("[data-agenda-custom-from]");
  const customTo = periodDialog?.querySelector("[data-agenda-custom-to]");
  const periodError = periodDialog?.querySelector("[data-agenda-period-error]");
  const unassignedDialog = document.querySelector("[data-agenda-unassigned-dialog]");
  const unassignedMessage = unassignedDialog?.querySelector("[data-agenda-unassigned-message]");
  let pendingPrintUrl = "";
  if (!page || !grid) return;

  const pad = value => String(value).padStart(2, "0");
  const iso = date => `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
  const italian = value => { const parts = String(value).split("-"); return parts.length === 3 ? `${parts[2]}/${parts[1]}/${parts[0]}` : ""; };
  const today = () => { const value = new Date(); value.setHours(12, 0, 0, 0); return value; };
  const addDays = (date, days) => { const value = new Date(date); value.setDate(value.getDate() + days); return value; };
  const addMonth = date => { const value = new Date(date); value.setMonth(value.getMonth() + 1); return value; };
  const norm = value => String(value ?? "").normalize("NFD").replace(/[\u0300-\u036f]/g, "").toLocaleLowerCase("it-IT");
  let range = { from: iso(today()), to: iso(today()) };
  let lastApplied = "today";

  const visibleRows = () => rows.filter(row => !row.hidden);
  const selectedRows = () => rows.filter(row => row.querySelector("[data-agenda-check]")?.checked);
  const updateSelection = () => {
    const selected = selectedRows().length;
    const visible = visibleRows();
    const visibleSelected = visible.filter(row => row.querySelector("[data-agenda-check]")?.checked).length;
    if (selectedCount) selectedCount.textContent = selected;
    printButtons.forEach(button => { button.disabled = selected === 0; });
    if (checkAll) {
      checkAll.checked = visible.length > 0 && visibleSelected === visible.length;
      checkAll.indeterminate = visibleSelected > 0 && visibleSelected < visible.length;
    }
  };
  const setQuick = value => {
    const from = today(); let to = from;
    if (value === "week") to = addDays(from, 7);
    else if (value === "15days") to = addDays(from, 15);
    else if (value === "month") to = addMonth(from);
    range = { from: iso(from), to: iso(to) };
    lastApplied = value;
    if (summary) summary.value = range.from === range.to ? italian(range.from) : `${italian(range.from)} - ${italian(range.to)}`;
  };
  const filter = () => {
    const term = norm(search?.value), state = norm(status?.value); let visible = 0;
    rows.forEach(row => {
      const rowState = norm(row.dataset.status), rowDate = row.dataset.date || "";
      row.hidden = !!((rowDate < range.from || rowDate > range.to) || (state && rowState !== state) || (term && !norm(row.dataset.filterText).includes(term)));
      if (!row.hidden) visible += 1;
    });
    if (count) count.textContent = visible;
    if (empty) empty.hidden = visible !== 0;
    clear?.toggleAttribute("hidden", !search?.value);
    updateSelection();
  };
  const openCustom = () => {
    if (!periodDialog || periodDialog.open) return;
    if (periodError) periodError.hidden = true;
    periodDialog.showModal();
    periodDialog.querySelector("[data-filter-date-display]")?.focus();
  };
  const applyCustom = () => {
    const from = customFrom?.value || "", to = customTo?.value || "";
    if (!from || !to || to < from) { if (periodError) periodError.hidden = false; return; }
    range = { from, to }; lastApplied = "custom";
    if (summary) summary.value = `${italian(from)} - ${italian(to)}`;
    periodDialog.close(); filter();
  };
  const openPrint = type => {
    const printRows = selectedRows();
    if (!printRows.length) return;
    const ids = printRows.map(row => row.dataset.workId).filter(Boolean).join(",");
    pendingPrintUrl = `/Interventi/Stampa?tipo=${encodeURIComponent(type)}&ids=${encodeURIComponent(ids)}`;
    const uncovered = printRows.filter(row => !(row.dataset.operator || "").trim()).length;
    if (!uncovered || !unassignedDialog) { location.href = pendingPrintUrl; return; }
    if (unassignedMessage) {
      const noun = uncovered === 1 ? "scheda" : "schede";
      const verb = uncovered === 1 ? "non ha" : "non hanno";
      unassignedMessage.innerHTML = `<strong>${uncovered} ${noun}</strong> ${verb} un operatore assegnato.`;
    }
    unassignedDialog.showModal();
    unassignedDialog.querySelector("[data-agenda-unassigned-confirm]")?.focus();
  };

  grid.querySelectorAll("[data-agenda-open]").forEach(button => button.addEventListener("click", event => {
    event.stopPropagation(); location.href = button.closest("[data-agenda-row]")?.dataset.openUrl || "";
  }));
  rows.forEach(row => {
    const checkbox = row.querySelector("[data-agenda-check]");
    checkbox?.addEventListener("click", event => event.stopPropagation());
    checkbox?.addEventListener("change", updateSelection);
    row.addEventListener("dblclick", () => { location.href = row.dataset.openUrl; });
    row.addEventListener("keydown", event => { if (event.key === "Enter") { event.preventDefault(); location.href = row.dataset.openUrl; } });
    row.addEventListener("click", event => {
      if (event.target.closest("button,input,a")) return;
      rows.forEach(item => item.classList.toggle("selected-row", item === row)); row.focus();
    });
  });
  checkAll?.addEventListener("change", () => { visibleRows().forEach(row => { row.querySelector("[data-agenda-check]").checked = checkAll.checked; }); updateSelection(); });
  page.querySelectorAll("[data-agenda-print]").forEach(button => button.addEventListener("click", () => openPrint(button.dataset.agendaPrint)));
  unassignedDialog?.querySelector("[data-agenda-unassigned-confirm]")?.addEventListener("click", () => { unassignedDialog.close(); location.href = pendingPrintUrl; });
  unassignedDialog?.querySelector("[data-agenda-unassigned-cancel]")?.addEventListener("click", () => { unassignedDialog.close(); pendingPrintUrl = ""; });
  period?.addEventListener("change", () => { if (period.value === "custom") { openCustom(); return; } setQuick(period.value); filter(); });
  period?.addEventListener("click", () => { if (period.value === "custom") openCustom(); });
  periodDialog?.querySelector("[data-agenda-period-apply]")?.addEventListener("click", applyCustom);
  periodDialog?.querySelector("[data-agenda-period-cancel]")?.addEventListener("click", () => { periodDialog.close(); period.value = lastApplied; });
  status?.addEventListener("change", filter);
  search?.addEventListener("input", filter);
  clear?.addEventListener("click", () => { search.value = ""; search.focus(); filter(); });
  setQuick("today"); filter(); updateSelection();
});

document.addEventListener("DOMContentLoaded", () => {
  const page = document.querySelector("[data-agenda-list-page]");
  const grid = page?.querySelector("[data-agenda-grid]");
  const rows = [...(grid?.querySelectorAll("[data-agenda-row]") ?? [])];
  const period = page?.querySelector("[data-agenda-period]");
  const summary = page?.querySelector("[data-agenda-period-summary]");
  const search = page?.querySelector("[data-agenda-search]");
  const status = page?.querySelector("[data-agenda-status]");
  const operatorFilter = page?.querySelector("[data-agenda-operator-filter]");
  const clear = page?.querySelector("[data-agenda-search-clear]");
  const count = page?.querySelector("[data-agenda-count]");
  const empty = page?.querySelector("[data-agenda-empty]");
  const checkAll = page?.querySelector("[data-agenda-check-all]");
  const selectedCount = page?.querySelector("[data-agenda-selected-count]");
  const printButtons = [...(page?.querySelectorAll("[data-agenda-print]") ?? [])];
  const dispatchButton = page?.querySelector("[data-agenda-dispatch]");
  const commandForm = document.querySelector("[data-agenda-command-form]");
  const requestToken = commandForm?.querySelector('input[name="__RequestVerificationToken"]')?.value || "";
  const operatorDialog = document.querySelector("[data-agenda-operator-dialog]");
  const operatorSelect = operatorDialog?.querySelector("[data-agenda-operator-select]");
  const operatorError = operatorDialog?.querySelector("[data-agenda-operator-error]");
  const dispatchDialog = document.querySelector("[data-agenda-dispatch-dialog]");
  const dispatchError = dispatchDialog?.querySelector("[data-agenda-dispatch-error]");
  const periodDialog = document.querySelector("[data-agenda-period-dialog]");
  const customFrom = periodDialog?.querySelector("[data-agenda-custom-from]");
  const customTo = periodDialog?.querySelector("[data-agenda-custom-to]");
  const periodError = periodDialog?.querySelector("[data-agenda-period-error]");
  const unassignedDialog = document.querySelector("[data-agenda-unassigned-dialog]");
  const unassignedMessage = unassignedDialog?.querySelector("[data-agenda-unassigned-message]");
  const stateKey = "skylab.agenda.state";
  let pendingPrintUrl = "";
  let pendingDispatchRow = null;
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

  const saveState = () => {
    const value = {
      range, period: period?.value || lastApplied, lastApplied,
      search: search?.value || "", status: status?.value || "", operator: operatorFilter?.value || "",
      scrollTop: grid.scrollTop, scrollLeft: grid.scrollLeft,
      printIds: selectedRows().map(row => row.dataset.workId),
      dispatchIds: dispatchRows().map(row => row.dataset.workId)
    };
    sessionStorage.setItem(stateKey, JSON.stringify(value));
  };
  const restoreState = () => {
    let value; try { value=JSON.parse(sessionStorage.getItem(stateKey)||"null"); } catch { value=null; }
    if(!value?.range?.from||!value?.range?.to)return false;
    range=value.range;lastApplied=value.lastApplied||value.period||"custom";
    if(period)period.value=value.period||lastApplied;
    if(summary)summary.value=range.from===range.to?italian(range.from):`${italian(range.from)} - ${italian(range.to)}`;
    if(search)search.value=value.search||"";if(status)status.value=value.status||"";if(operatorFilter)operatorFilter.value=value.operator||"";
    const printIds=new Set(value.printIds||[]),dispatchIds=new Set(value.dispatchIds||[]);
    rows.forEach(row=>{const print=row.querySelector("[data-agenda-check]"),dispatch=row.querySelector("[data-agenda-dispatch-check]");if(print)print.checked=printIds.has(row.dataset.workId);if(dispatch&&!dispatch.disabled)dispatch.checked=dispatchIds.has(row.dataset.workId);});
    requestAnimationFrame(()=>{grid.scrollTop=Number(value.scrollTop)||0;grid.scrollLeft=Number(value.scrollLeft)||0;});
    return true;
  };

  const visibleRows = () => rows.filter(row => !row.hidden);
  const selectedRows = () => rows.filter(row => row.querySelector("[data-agenda-check]")?.checked);
  const dispatchRows = () => rows.filter(row => row.dataset.dispatched !== "true" && row.querySelector("[data-agenda-dispatch-check]")?.checked);
  const updateDispatch = () => {
    const selected=dispatchRows();
    rows.forEach(row=>row.classList.toggle("agenda-row-ready",row.dataset.dispatched!=="true"&&row.querySelector("[data-agenda-dispatch-check]")?.checked));
    if(dispatchButton)dispatchButton.disabled=selected.length===0||selected.some(row=>!row.dataset.operatorId||row.dataset.operatorId==="0");
  };
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
    updateDispatch();
  };

  const responseMessage=async response=>{try{const body=await response.json();return body.message||"Operazione non riuscita.";}catch{return "Operazione non riuscita.";}};
  const assignOperator=async()=>{
    if(!pendingDispatchRow||!operatorSelect?.value){if(operatorError)operatorError.hidden=false;return;}
    if(operatorError)operatorError.hidden=true;
    const confirm=operatorDialog.querySelector("[data-agenda-operator-confirm]");if(confirm)confirm.disabled=true;
    try{
      const body=new URLSearchParams({workId:pendingDispatchRow.dataset.workId,operatorId:operatorSelect.value,__RequestVerificationToken:requestToken});
      const response=await fetch("/Interventi/Index?handler=AssignOperator",{method:"POST",headers:{"Content-Type":"application/x-www-form-urlencoded;charset=UTF-8"},body});
      if(!response.ok)throw new Error(await responseMessage(response));
      const label=operatorSelect.options[operatorSelect.selectedIndex]?.text||"";
      pendingDispatchRow.dataset.operatorId=operatorSelect.value;pendingDispatchRow.dataset.operator=label;
      const cell=pendingDispatchRow.querySelector("[data-agenda-operator-name]");if(cell)cell.textContent=label;
      operatorDialog.close();pendingDispatchRow=null;updateDispatch();
    }catch(error){if(operatorError){operatorError.textContent=error.message;operatorError.hidden=false;}}
    finally{if(confirm)confirm.disabled=false;}
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
    const term = norm(search?.value), state = norm(status?.value), operator = operatorFilter?.value || ""; let visible = 0;
    rows.forEach(row => {
      const rowState = norm(row.dataset.status), rowDate = row.dataset.date || "";
      const rowOperator = row.dataset.operatorId || "0";
      row.hidden = !!((rowDate < range.from || rowDate > range.to) || (state && rowState !== state) || (operator && rowOperator !== operator) || (term && !norm(row.dataset.filterText).includes(term)));
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
    const setDialogDate = (hidden, value) => {
      if (!hidden) return;
      hidden.value = value || "";
      const display = hidden.closest("[data-date-control]")?.querySelector("[data-filter-date-display]");
      if (display) display.value = italian(value);
    };
    setDialogDate(customFrom, range.from);
    setDialogDate(customTo, range.to);
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
    event.stopPropagation(); saveState(); location.href = button.closest("[data-agenda-row]")?.dataset.openUrl || "";
  }));
  rows.forEach(row => {
    const checkbox = row.querySelector("[data-agenda-check]");
    checkbox?.addEventListener("click", event => event.stopPropagation());
    checkbox?.addEventListener("change", updateSelection);
    const dispatchCheck=row.querySelector("[data-agenda-dispatch-check]");
    dispatchCheck?.addEventListener("click",event=>event.stopPropagation());
    dispatchCheck?.addEventListener("change",()=>{
      if(dispatchCheck.checked&&(!row.dataset.operatorId||row.dataset.operatorId==="0")){
        pendingDispatchRow=row;if(operatorSelect)operatorSelect.value="";if(operatorError){operatorError.textContent="Selezionare un operatore.";operatorError.hidden=true;}operatorDialog?.showModal();operatorSelect?.focus();
      }
      updateDispatch();
    });
    row.addEventListener("dblclick", () => { saveState(); location.href = row.dataset.openUrl; });
    row.addEventListener("keydown", event => { if (event.key === "Enter") { event.preventDefault(); saveState(); location.href = row.dataset.openUrl; } });
    row.addEventListener("click", event => {
      if (event.target.closest("button,input,a")) return;
      rows.forEach(item => item.classList.toggle("selected-row", item === row)); row.focus();
    });
  });
  checkAll?.addEventListener("change", () => { visibleRows().forEach(row => { row.querySelector("[data-agenda-check]").checked = checkAll.checked; }); updateSelection(); });
  page.querySelectorAll("[data-agenda-print]").forEach(button => button.addEventListener("click", () => openPrint(button.dataset.agendaPrint)));
  unassignedDialog?.querySelector("[data-agenda-unassigned-confirm]")?.addEventListener("click", () => { unassignedDialog.close(); location.href = pendingPrintUrl; });
  unassignedDialog?.querySelector("[data-agenda-unassigned-cancel]")?.addEventListener("click", () => { unassignedDialog.close(); pendingPrintUrl = ""; });
  operatorDialog?.querySelector("[data-agenda-operator-confirm]")?.addEventListener("click",assignOperator);
  operatorDialog?.querySelector("[data-agenda-operator-cancel]")?.addEventListener("click",()=>{if(pendingDispatchRow)pendingDispatchRow.querySelector("[data-agenda-dispatch-check]").checked=false;pendingDispatchRow=null;operatorDialog.close();updateDispatch();});
  dispatchButton?.addEventListener("click",()=>{const selected=dispatchRows();if(!selected.length)return;const message=dispatchDialog?.querySelector("[data-agenda-dispatch-message]");if(message)message.innerHTML=`Stai per scaricare alla lavorazione <strong>${selected.length} ${selected.length===1?"scheda":"schede"}</strong>.`;if(dispatchError)dispatchError.hidden=true;dispatchDialog?.showModal();});
  dispatchDialog?.querySelector("[data-agenda-dispatch-cancel]")?.addEventListener("click",()=>dispatchDialog.close());
  dispatchDialog?.querySelector("[data-agenda-dispatch-confirm]")?.addEventListener("click",async()=>{const ids=dispatchRows().map(row=>Number(row.dataset.workId));const confirm=dispatchDialog.querySelector("[data-agenda-dispatch-confirm]");confirm.disabled=true;try{const response=await fetch("/Interventi/Index?handler=Dispatch",{method:"POST",headers:{"Content-Type":"application/json","RequestVerificationToken":requestToken},body:JSON.stringify(ids)});if(!response.ok)throw new Error(await responseMessage(response));location.reload();}catch(error){if(dispatchError){dispatchError.textContent=error.message;dispatchError.hidden=false;}}finally{confirm.disabled=false;}});
  period?.addEventListener("change", () => { if (period.value === "custom") { openCustom(); return; } setQuick(period.value); filter(); });
  period?.addEventListener("click", () => { if (period.value === "custom") openCustom(); });
  periodDialog?.querySelector("[data-agenda-period-apply]")?.addEventListener("click", applyCustom);
  periodDialog?.querySelector("[data-agenda-period-cancel]")?.addEventListener("click", () => { periodDialog.close(); period.value = lastApplied; });
  status?.addEventListener("change", filter);
  operatorFilter?.addEventListener("change", filter);
  search?.addEventListener("input", filter);
  clear?.addEventListener("click", () => { search.value = ""; search.focus(); filter(); });
  window.addEventListener("pagehide",saveState);
  if(!restoreState())setQuick("today");filter();updateSelection();
});

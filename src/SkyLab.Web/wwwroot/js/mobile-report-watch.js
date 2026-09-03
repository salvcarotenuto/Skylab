document.addEventListener("DOMContentLoaded", () => {
  const toast = document.querySelector("[data-mobile-report-toast]");
  if (!toast) return;

  const storageKey = "skylab.mobileReports.notified.v2";
  let notified;
  try { notified = new Set(JSON.parse(sessionStorage.getItem(storageKey) || "[]").map(Number)); }
  catch { notified = new Set(); }
  let startTimer = 0, pollTimer = 0, closeTimer = 0, controller = null;

  const saveNotified = () => sessionStorage.setItem(storageKey, JSON.stringify([...notified].slice(-250)));
  const closeToast = () => {
    if (closeTimer) clearTimeout(closeTimer);
    closeTimer = 0;
    toast.hidden = true;
  };
  const examineUrl = item => `/Lavori/Scheda?id=${item.workId}&azione=103#consuntivo`;
  const showToast = item => {
    toast.querySelector("[data-mobile-report-work]").textContent = `Scheda ${item.workNumber}`;
    toast.querySelector("[data-mobile-report-customer]").textContent = item.customer || "";
    toast.querySelector("[data-mobile-report-operator]").textContent = item.username ? `Operatore ${item.username}` : "";
    toast.querySelector("[data-mobile-report-open]").onclick = () => { location.href = examineUrl(item); };
    toast.hidden = false;
    if (closeTimer) clearTimeout(closeTimer);
    closeTimer = setTimeout(closeToast, 10000);
  };
  const updateAgendaRow = item => {
    const row = document.querySelector(`[data-agenda-row][data-work-id="${item.workId}"]`);
    if (!row) return;
    row.dataset.flow = item.sheetFlow;
    const workStatus = row.querySelector("[data-agenda-work-status]");
    if (workStatus) workStatus.textContent = item.workStatus;
    const flow = row.querySelector("[data-agenda-flow]");
    if (flow) {
      flow.textContent = item.sheetFlow;
      flow.className = `agenda-flow${item.sheetFlow === "Da confermare" ? " is-received" : item.sheetFlow === "Confermato" ? " is-acquired" : item.sheetFlow === "Errore" ? " is-error" : ""}`;
    }
    const button = row.querySelector("[data-agenda-open]");
    if (button) button.textContent = "Apri";
    row.dataset.openUrl = item.sheetFlow === "Da confermare" ? examineUrl(item) : `/Lavori/Scheda?id=${item.workId}&azione=103`;
  };
  const poll = async () => {
    controller?.abort();
    controller = new AbortController();
    try {
      const response = await fetch("/Interventi/Index?handler=Flow", { headers: { Accept: "application/json" }, cache: "no-store", signal: controller.signal });
      if (!response.ok) return;
      const items = await response.json();
      items.forEach(updateAgendaRow);
      const unseen = items.filter(item => item.sheetFlow === "Da confermare" && item.inboxId && !notified.has(Number(item.inboxId)));
      if (unseen.length) {
        unseen.forEach(item => notified.add(Number(item.inboxId)));
        saveNotified();
        const item = unseen[0];
        const row = document.querySelector(`[data-agenda-row][data-work-id="${item.workId}"]`);
        if (row) { row.classList.remove("agenda-row-report-received"); void row.offsetWidth; row.classList.add("agenda-row-report-received"); }
        showToast(item);
      }
    } catch (error) {
      if (error.name !== "AbortError") { /* Il controllo successivo riproverà. */ }
    }
  };
  const stop = () => {
    clearTimeout(startTimer); clearInterval(pollTimer); clearTimeout(closeTimer);
    controller?.abort(); startTimer = pollTimer = closeTimer = 0;
  };

  toast.querySelectorAll("[data-mobile-report-close]").forEach(button => button.addEventListener("click", closeToast));
  startTimer = setTimeout(() => { poll(); pollTimer = setInterval(poll, 30 * 1000); }, 5000);
  window.addEventListener("pagehide", stop, { once: true });
});

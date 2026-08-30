document.addEventListener("DOMContentLoaded", () => {
  const fields = Array.from(document.querySelectorAll("[data-filter-date-display]"));
  if (!fields.length) return;
  let panel;
  let activeField;
  let activeMonth;
  const pad = (value) => String(value).padStart(2, "0");
  const valid = (day, month, year) => {
    const date = new Date(year, month - 1, day);
    return date.getFullYear() === year && date.getMonth() === month - 1 && date.getDate() === day;
  };
  const iso = (value) => {
    const match = /^(\d{1,2})[/-](\d{1,2})[/-](\d{4})$/.exec(String(value ?? "").trim());
    if (!match) return "";
    const day = Number(match[1]); const month = Number(match[2]); const year = Number(match[3]);
    return valid(day, month, year) ? `${year}-${pad(month)}-${pad(day)}` : "";
  };
  const dateValue = (field) => {
    const value = iso(field.value);
    if (!value) return null;
    const [year, month, day] = value.split("-").map(Number);
    return new Date(year, month - 1, day);
  };
  const hidden = (field) => field.closest("[data-date-control]")?.querySelector("[data-filter-date-hidden]");
  const selectedDatePart = (field) => (field.selectionStart ?? 0) <= 2 ? "day" : (field.selectionStart ?? 0) <= 5 ? "month" : "year";
  const selectDatePart = (field, part) => {
    const ranges = { day: [0, 2], month: [3, 5], year: [6, 10] };
    window.requestAnimationFrame(() => field.setSelectionRange(...ranges[part]));
  };
  const sync = (field) => {
    const value = iso(field.value); const target = hidden(field);
    if (!value || !target) return false;
    const [year, month, day] = value.split("-");
    field.value = `${day}/${month}/${year}`;
    if (target.value !== value) {
      target.value = value;
      target.dispatchEvent(new Event("change", { bubbles: true }));
    }
    return true;
  };
  const shiftDatePart = (field, direction) => {
    const current = dateValue(field);
    if (!current) return false;
    const part = selectedDatePart(field);
    const year = current.getFullYear(); const month = current.getMonth(); const day = current.getDate();
    let shifted;
    if (part === "day") {
      const lastDay = new Date(year, month + 1, 0).getDate();
      shifted = new Date(year, month, Math.max(1, Math.min(day + direction, lastDay)));
    } else if (part === "month") {
      const targetMonth = new Date(year, month + direction, 1);
      const lastDay = new Date(targetMonth.getFullYear(), targetMonth.getMonth() + 1, 0).getDate();
      shifted = new Date(targetMonth.getFullYear(), targetMonth.getMonth(), Math.min(day, lastDay));
    } else {
      const targetYear = year + direction;
      const lastDay = new Date(targetYear, month + 1, 0).getDate();
      shifted = new Date(targetYear, month, Math.min(day, lastDay));
    }
    field.value = `${pad(shifted.getDate())}/${pad(shifted.getMonth() + 1)}/${shifted.getFullYear()}`;
    sync(field);
    selectDatePart(field, part);
    return true;
  };
  const ensurePanel = () => {
    if (!panel) {
      panel = document.createElement("div");
      panel.className = "micronote-date-picker";
      panel.hidden = true;
      panel.setAttribute("role", "dialog");
      panel.setAttribute("aria-label", "Calendario");
      panel.addEventListener("click", (event) => {
        const dayButton = event.target.closest?.("[data-date-picker-day]");
        if (!dayButton || !activeField) return;
        event.preventDefault();
        event.stopImmediatePropagation();
        const field = activeField;
        field.value = `${pad(dayButton.dataset.datePickerDay)}/${pad(activeMonth.getMonth() + 1)}/${activeMonth.getFullYear()}`;
        sync(field);
        const target = hidden(field) ?? field.previousElementSibling;
        const value = iso(field.value);
        if (target && value && target.value !== value) {
          target.value = value;
          target.dispatchEvent(new Event("change", { bubbles: true }));
          target.value = value;
          target.setAttribute("value", value);
        }
        close();
      }, true);
      document.body.appendChild(panel);
    }
    return panel;
  };
  const isOpen = () => Boolean(panel && !panel.hidden);
  const close = () => { if (panel) panel.hidden = true; activeField = null; };
  const position = () => {
    if (!panel || !activeField) return;
    const rect = activeField.getBoundingClientRect();
    panel.style.position = "fixed";
    panel.style.inset = "auto";
    panel.style.margin = "0";
    panel.style.left = `${Math.max(8, rect.right - panel.offsetWidth)}px`;
    panel.style.top = `${rect.bottom + 4}px`;
  };
  const render = () => {
    const box = ensurePanel();
    const selected = dateValue(activeField);
    const year = activeMonth.getFullYear(); const month = activeMonth.getMonth();
    const offset = (new Date(year, month, 1).getDay() + 6) % 7;
    const count = new Date(year, month + 1, 0).getDate();
    const cells = Array.from({ length: offset }, () => '<span class="micronote-date-picker-empty"></span>');
    for (let day = 1; day <= count; day += 1) {
      const chosen = selected && selected.getFullYear() === year && selected.getMonth() === month && selected.getDate() === day;
      cells.push(`<button type="button" class="${chosen ? "is-selected" : ""}" data-date-picker-day="${day}">${day}</button>`);
    }
    box.innerHTML = `<div class="micronote-date-picker-head"><button type="button" data-date-picker-prev>&lt;</button><strong>${activeMonth.toLocaleDateString("it-IT", { month: "long", year: "numeric" })}</strong><button type="button" data-date-picker-next>&gt;</button></div><div class="micronote-date-picker-weekdays">${["Lu","Ma","Me","Gi","Ve","Sa","Do"].map(day => `<span>${day}</span>`).join("")}</div><div class="micronote-date-picker-days">${cells.join("")}</div>`;
  };
  const open = (field) => {
    activeField = field;
    (field.closest("dialog") ?? document.body).appendChild(ensurePanel());
    const base = dateValue(field) ?? new Date();
    activeMonth = new Date(base.getFullYear(), base.getMonth(), 1);
    render();
    panel.hidden = false;
    position(); field.focus();
  };
  fields.forEach((field) => {
    field.addEventListener("input", () => {
      const digits = field.value.replace(/\D/g, "").slice(0, 8);
      field.value = digits.length > 4 ? `${digits.slice(0, 2)}/${digits.slice(2, 4)}/${digits.slice(4)}` : digits.length > 2 ? `${digits.slice(0, 2)}/${digits.slice(2)}` : digits;
    });
    field.addEventListener("change", () => sync(field));
    field.addEventListener("blur", () => sync(field));
    field.addEventListener("focus", () => { if (dateValue(field)) selectDatePart(field, "day"); });
    field.addEventListener("click", () => { if (dateValue(field)) selectDatePart(field, selectedDatePart(field)); });
    field.addEventListener("keydown", (event) => {
      if (event.key === "ArrowUp" || event.key === "ArrowDown") {
        event.preventDefault(); shiftDatePart(field, event.key === "ArrowUp" ? 1 : -1); return;
      }
      if (event.key === "ArrowLeft" || event.key === "ArrowRight") {
        const part = selectedDatePart(field);
        const nextPart = event.key === "ArrowLeft" ? part === "year" ? "month" : "day" : part === "day" ? "month" : "year";
        event.preventDefault(); selectDatePart(field, nextPart); return;
      }
      if (event.key === "Enter") { event.preventDefault(); sync(field); }
    });
  });
  document.addEventListener("click", (event) => {
    const target = event.target;
    const trigger = target.closest?.("[data-date-picker-button]");
    if (trigger) {
      event.preventDefault();
      const field = trigger.closest("[data-date-control]")?.querySelector("[data-filter-date-display]");
      if (field) activeField === field && isOpen() ? close() : open(field);
      return;
    }
    if (target.closest?.(".micronote-date-picker")) {
      event.preventDefault();
      if (target.closest("[data-date-picker-prev]")) activeMonth = new Date(activeMonth.getFullYear(), activeMonth.getMonth() - 1, 1);
      else if (target.closest("[data-date-picker-next]")) activeMonth = new Date(activeMonth.getFullYear(), activeMonth.getMonth() + 1, 1);
      else {
        const dayButton = target.closest("[data-date-picker-day]");
        if (dayButton) {
          const field = activeField;
          field.value = `${pad(dayButton.dataset.datePickerDay)}/${pad(activeMonth.getMonth() + 1)}/${activeMonth.getFullYear()}`;
          sync(field);
          const dateTarget = hidden(field) ?? field.previousElementSibling;
          const dateIso = iso(field.value);
          if (dateTarget && dateIso) {
            dateTarget.value = dateIso;
            dateTarget.setAttribute("value", dateIso);
            dateTarget.dispatchEvent(new Event("change", { bubbles: true }));
            dateTarget.value = dateIso;
          }
          close();
          return;
        }
      }
      render(); position(); return;
    }
    if (!target.closest?.("[data-date-control]")) close();
  });
  document.addEventListener("keydown", (event) => {
    if (event.key === "Escape" && isOpen()) { event.preventDefault(); event.stopImmediatePropagation(); close(); }
  }, true);
});

document.addEventListener("DOMContentLoaded", () => {
  const dialog = document.querySelector("[data-acquire-dialog]");
  const date = dialog?.querySelector("[data-acquire-date-hidden]");
  const title = dialog?.querySelector("[data-availability-title]");
  if (!date || !title) return;

  dialog.querySelector("form")?.addEventListener("submit", () => {
    const display = dialog.querySelector("[data-acquire-date-display]");
    const match = /^(\d{2})\/(\d{2})\/(\d{4})$/.exec(display?.value ?? "");
    if (match) date.value = `${match[3]}-${match[2]}-${match[1]}`;
  });

  [
    ["[data-active-operators]", "Operatori attivi"],
    ["[data-assigned-operators]", "Operatori impegnati"],
    ["[data-reservations]", "Lavori prenotati"],
    ["[data-planned-works]", "Lavori pianificati"]
  ].forEach(([selector, label]) => {
    const value = dialog.querySelector(selector);
    const caption = value?.previousElementSibling;
    if (caption) caption.textContent = label;
  });

  const metrics = dialog.querySelector(".availability-metrics");
  const agendaButton = dialog.querySelector("[data-agenda-open]");
  if (metrics && agendaButton) {
    const agendaRow = document.createElement("div");
    agendaRow.className = "availability-agenda-row";
    metrics.insertAdjacentElement("afterend", agendaRow);
    agendaRow.append(agendaButton);
  }

  const easter = (year) => {
    const a = year % 19, b = Math.floor(year / 100), c = year % 100;
    const d = Math.floor(b / 4), e = b % 4, f = Math.floor((b + 8) / 25);
    const g = Math.floor((b - f + 1) / 3), h = (19 * a + b - d - g + 15) % 30;
    const i = Math.floor(c / 4), k = c % 4, l = (32 + 2 * e + 2 * i - h - k) % 7;
    const m = Math.floor((a + 11 * h + 22 * l) / 451);
    const month = Math.floor((h + l - 7 * m + 114) / 31);
    const day = (h + l - 7 * m + 114) % 31 + 1;
    return new Date(year, month - 1, day, 12);
  };
  const isClosed = (value) => {
    if (value.getDay() === 0 || value.getDay() === 6) return true;
    const key = `${value.getMonth() + 1}-${value.getDate()}`;
    if (["1-1", "1-6", "4-25", "5-1", "6-2", "8-15", "11-1", "12-8", "12-25", "12-26"].includes(key)) return true;
    const monday = easter(value.getFullYear());
    monday.setDate(monday.getDate() + 1);
    return value.toDateString() === monday.toDateString();
  };
  document.querySelectorAll("[data-acquire-open][data-agreed]").forEach(button => {
    if (!button.dataset.agreed) return;
    const agreed = new Date(`${button.dataset.agreed}T12:00:00`);
    const rowDate = button.closest(".due-asset")?.querySelector(":scope > div:nth-child(4) > strong");
    if (rowDate && isClosed(agreed)) rowDate.classList.add("availability-nonworking");
  });
  const updateTitle = () => {
    title.classList.remove("availability-nonworking");
    if (!date.value) { title.textContent = "Disponibilità per il giorno"; return; }
    const value = new Date(`${date.value}T12:00:00`);
    const weekday = value.toLocaleDateString("it-IT", { weekday: "long" });
    const label = weekday.charAt(0).toUpperCase() + weekday.slice(1);
    title.textContent = "Disponibilità per il giorno ";
    const detail = document.createElement("span");
    detail.textContent = `${label} ${value.toLocaleDateString("it-IT")}`;
    if (isClosed(value)) detail.classList.add("availability-nonworking");
    title.append(detail);
  };
  const updateAfterPageHandlers = () => setTimeout(updateTitle, 10);
  date.addEventListener("change", updateAfterPageHandlers);
  document.addEventListener("click", (event) => {
    if (event.target.closest?.("[data-acquire-open]")) updateAfterPageHandlers();
  });
  updateAfterPageHandlers();
});

document.addEventListener("DOMContentLoaded", () => {
  const agenda = document.querySelector("[data-agenda-dialog]");
  const toHidden = agenda?.querySelector("[data-agenda-to]");
  const toDisplay = agenda?.querySelector("[data-agenda-to-display]");
  const addButton = agenda?.querySelector("[data-agenda-load]");
  const rows = agenda?.querySelector("[data-agenda-rows]");
  const grid = agenda?.querySelector(".agenda-grid");
  if (!agenda || !toHidden || !toDisplay || !addButton || !rows || !grid) return;

  grid.tabIndex = 0;
  new MutationObserver(() => {
    if (agenda.open) requestAnimationFrame(() => grid.focus({ preventScroll: true }));
  }).observe(agenda, { attributes: true, attributeFilter: ["open"] });

  const legacyRange = document.createElement("p");
  legacyRange.hidden = true;
  legacyRange.dataset.agendaRange = "";
  agenda.append(legacyRange);

  const pad = (value) => String(value).padStart(2, "0");
  const formatDate = (value) => {
    const parts = String(value).slice(0, 10).split("-");
    return parts.length === 3 ? `${parts[2]}/${parts[1]}/${parts[0]}` : "";
  };
  const escape = (value) => String(value ?? "").replace(/[&<>"']/g, character => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" })[character]);
  const loadAgenda = async () => {
    rows.innerHTML = '<div class="agenda-empty">Caricamento…</div>';
    try {
      const response = await fetch(`/Lavori/Pianificazione?handler=Agenda&to=${encodeURIComponent(toHidden.value)}`);
      if (!response.ok) throw new Error();
      const items = await response.json();
      rows.innerHTML = items.length ? items.map(item => `<div class="agenda-grid-row"><strong>${formatDate(item.date)}</strong><span>${escape(item.time)}</span><span>${escape(item.customer)}</span><span>${escape(item.site)}</span><span>${escape(item.description)}</span><span>${escape(item.operator || "Da assegnare")}</span><span class="agenda-kind">${escape(item.kind)}</span></div>`).join("") : '<div class="agenda-empty">Nessun impegno nel periodo esaminato.</div>';
    } catch {
      rows.innerHTML = '<div class="agenda-empty">Agenda non disponibile.</div>';
    }
  };
  addButton.addEventListener("click", (event) => {
    event.preventDefault();
    event.stopImmediatePropagation();
    const base = toHidden.value ? new Date(`${toHidden.value}T12:00:00`) : new Date();
    base.setDate(base.getDate() + 30);
    toHidden.value = `${base.getFullYear()}-${pad(base.getMonth() + 1)}-${pad(base.getDate())}`;
    toDisplay.value = formatDate(toHidden.value);
    loadAgenda();
  }, true);
  document.addEventListener("click", (event) => {
    const open = event.target.closest?.("[data-agenda-open]");
    if (!open) return;
    event.preventDefault();
    event.stopImmediatePropagation();
    const bookingDate = document.querySelector("[data-acquire-date-hidden]")?.value;
    const end = bookingDate ? new Date(`${bookingDate}T12:00:00`) : new Date();
    end.setDate(end.getDate() + 60);
    toHidden.value = `${end.getFullYear()}-${pad(end.getMonth() + 1)}-${pad(end.getDate())}`;
    toDisplay.value = formatDate(toHidden.value);
    agenda.showModal();
    loadAgenda();
  }, true);

  const completeRows = () => {
    const actualRows = [...rows.children].filter(row => !row.classList.contains("agenda-placeholder") && !row.classList.contains("agenda-empty"));
    if (actualRows.some(row => row.children.length !== 7)) {
      loadAgenda();
      return;
    }
    const actual = actualRows.length;
    const wanted = Math.max(0, 12 - actual);
    const placeholders = [...rows.querySelectorAll(".agenda-placeholder")];
    if (placeholders.length === wanted) return;
    placeholders.forEach(row => row.remove());
    for (let index = 0; index < wanted; index += 1) {
      const row = document.createElement("div");
      row.className = "agenda-grid-row agenda-placeholder";
      row.setAttribute("aria-hidden", "true");
      row.innerHTML = "<span></span><span></span><span></span><span></span><span></span><span></span><span></span>";
      rows.append(row);
    }
  };
  new MutationObserver(completeRows).observe(rows, { childList: true });
  completeRows();
});

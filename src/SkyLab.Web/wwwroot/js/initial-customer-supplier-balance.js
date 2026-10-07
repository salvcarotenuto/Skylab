document.addEventListener("DOMContentLoaded", () => {
  const page = document.querySelector("[data-initial-cf-balance]");
  const form = page?.querySelector("[data-initial-cf-form]");
  const payload = form?.querySelector("[data-initial-cf-payload]");
  const yearSelect = form?.querySelector("[data-initial-cf-year]");
  const tables = Array.from(page?.querySelectorAll("[data-initial-cf-table]") ?? []);
  const rows = Array.from(page?.querySelectorAll("[data-initial-cf-row]") ?? []);
  const exitLink = page?.querySelector("[data-initial-cf-exit]");
  const clearButton = page?.querySelector("[data-initial-cf-clear]");
  const saveButton = page?.querySelector("[data-initial-cf-save]");
  const loadedYear = yearSelect?.value ?? "";
  let modified = false;
  let activeRow = page?.querySelector("[data-initial-cf-row].selected-row") ?? null;

  if (!page || !form || !payload || tables.length === 0) {
    return;
  }

  const normalize = (value) => String(value ?? "")
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .toLocaleLowerCase("it-IT")
    .trim();

  const parseDecimal = (value) => {
    const text = String(value ?? "").trim();
    const sign = text.startsWith("-") ? -1 : 1;
    const unsigned = text.replace(/^[+-]/, "");
    const commaIndex = unsigned.lastIndexOf(",");
    const dotIndex = unsigned.lastIndexOf(".");
    const decimalIndex = Math.max(commaIndex, dotIndex);
    const normalized = decimalIndex >= 0
      ? `${unsigned.slice(0, decimalIndex).replace(/[.,]/g, "")}.${unsigned.slice(decimalIndex + 1).replace(/[.,]/g, "")}`
      : unsigned.replace(/[.,]/g, "");
    const parsed = Number.parseFloat(normalized);
    return Number.isFinite(parsed) ? parsed * sign : 0;
  };

  const formatMoney = (value) => {
    const number = Number(value || 0);
    if (!Number.isFinite(number) || number === 0) {
      return "";
    }

    const fixed = Math.abs(number).toFixed(2);
    const [integerPart, decimalPart] = fixed.split(".");
    const sign = number < 0 ? "-" : "";
    const groupedInteger = integerPart.replace(/\B(?=(\d{3})+(?!\d))/g, ".");
    return `${sign}${groupedInteger},${decimalPart}`;
  };

  const formatTotal = (value) => {
    const formatted = formatMoney(value);
    return formatted || "0,00";
  };

  const formatForEdit = (value) => {
    const number = Number(value || 0);
    return number === 0 ? "" : String(Math.round(number * 100) / 100).replace(".", ",");
  };

  const cleanBalance = (input) => {
    const original = String(input.value ?? "").trim();
    const sign = original.startsWith("-") ? "-" : "";
    const text = original.replace(/[^\d.,]/g, "");
    const separatorMatches = Array.from(text.matchAll(/[.,]/g));
    const decimalIndex = separatorMatches.length > 1
      ? separatorMatches[separatorMatches.length - 1].index
      : (separatorMatches[0]?.index ?? -1);

    input.value = decimalIndex >= 0
      ? `${sign}${text.slice(0, decimalIndex).replace(/[.,]/g, "").slice(0, 10)},${text.slice(decimalIndex + 1).replace(/[.,]/g, "").slice(0, 2)}`
      : `${sign}${text.replace(/[.,]/g, "").slice(0, 10)}`;
  };

  const dataKey = (key) => `sort${key[0].toUpperCase()}${key.slice(1)}`;

  const selectedRow = () => page.querySelector("[data-initial-cf-row].selected-row");
  const tableRows = (table) => Array.from(table.querySelectorAll("[data-initial-cf-row]"));
  const visibleRows = (table) => tableRows(table).filter((row) => !row.hidden);

  const totals = {
    C: rows.filter((row) => row.dataset.cfType === "C")
      .reduce((sum, row) => sum + (Number.parseFloat(row.dataset.sortBalance ?? "0") || 0), 0),
    F: rows.filter((row) => row.dataset.cfType === "F")
      .reduce((sum, row) => sum + (Number.parseFloat(row.dataset.sortBalance ?? "0") || 0), 0)
  };

  const updateTotals = () => {
    ["C", "F"].forEach((type) => {
      const target = page.querySelector(`[data-initial-cf-total="${type}"]`);
      if (target) {
        target.value = formatTotal(totals[type]);
      }
    });
  };

  const updateRowBalanceSort = (input) => {
    const row = input.closest("[data-initial-cf-row]");
    if (row) {
      const previous = Number.parseFloat(row.dataset.sortBalance ?? "0") || 0;
      const next = parseDecimal(input.value);
      totals[row.dataset.cfType] += next - previous;
      row.dataset.sortBalance = String(next);
    }
    updateTotals();
  };

  const commitBalance = (input) => {
    cleanBalance(input);
    input.value = formatMoney(parseDecimal(input.value));
  };

  const gridFrame = (row) => row.closest(".initial-cf-grid-frame");
  const tableForRow = (row) => row.closest("[data-initial-cf-table]");

  const ensureVisible = (row, direction = 0) => {
    const frame = gridFrame(row);
    const table = tableForRow(row);
    const headerHeight = table.tHead?.offsetHeight ?? 0;
    const visibleTop = frame.scrollTop + headerHeight;
    const visibleBottom = frame.scrollTop + frame.clientHeight;
    const rowTop = row.offsetTop;
    const rowBottom = rowTop + row.offsetHeight;

    if (rowTop >= visibleTop && rowBottom <= visibleBottom) {
      return;
    }

    if (direction >= 0 && rowBottom > visibleBottom) {
      frame.scrollTop = rowBottom - frame.clientHeight + 1;
      return;
    }

    if (direction <= 0 && rowTop < visibleTop) {
      frame.scrollTop = Math.max(rowTop - headerHeight - 1, 0);
    }
  };

  const selectRow = (row, direction = 0) => {
    if (!row) {
      return;
    }

    if (activeRow !== row) {
      activeRow?.classList.remove("selected-row");
      activeRow?.removeAttribute("aria-selected");
      activeRow = row;
    }

    row.classList.add("selected-row");
    row.setAttribute("aria-selected", "true");
    ensureVisible(row, direction);
  };

  const focusBalance = (row, direction = 0) => {
    const input = row?.querySelector("[data-initial-cf-balance-input]");
    if (!input) {
      return;
    }

    selectRow(row, direction);
    input.focus({ preventScroll: true });
    input.select();
  };

  const selectVisibleRowFromScroll = (table, delta) => {
    if (delta === 0) {
      return;
    }

    const selected = selectedRow();
    const frame = table.closest(".initial-cf-grid-frame");
    const headerHeight = table.tHead?.offsetHeight ?? 0;
    const visibleTop = frame.scrollTop + headerHeight;
    const visibleBottom = frame.scrollTop + frame.clientHeight;
    if (!selected || tableForRow(selected) !== table
        || (selected.offsetTop + selected.offsetHeight > visibleTop && selected.offsetTop < visibleBottom)) {
      return;
    }

    const candidates = table.tBodies[0]?.rows;
    if (!candidates?.length) {
      return;
    }

    const firstRowEndingAfter = (offset) => {
      let low = 0;
      let high = candidates.length;
      while (low < high) {
        const middle = (low + high) >> 1;
        const row = candidates[middle];
        if (row.offsetTop + row.offsetHeight <= offset) {
          low = middle + 1;
        } else {
          high = middle;
        }
      }
      return low;
    };

    let candidateIndex = firstRowEndingAfter(delta > 0 ? visibleTop : visibleBottom);
    if (delta > 0 && candidateIndex < candidates.length && candidates[candidateIndex].offsetTop < visibleTop) {
      candidateIndex += 1;
    } else if (delta < 0) {
      candidateIndex -= 1;
    }

    const candidate = candidateIndex >= 0 ? candidates[candidateIndex] : null;
    if (candidate && !candidate.hidden) {
      focusBalance(candidate);
    }
  };

  const moveSelection = (row, offset) => {
    const rowsInTable = tableForRow(row)?.tBodies[0]?.rows;
    const nextRow = rowsInTable?.[Math.max(0, Math.min(row.sectionRowIndex + offset, rowsInTable.length - 1))];
    if (!nextRow || nextRow.hidden) {
      return false;
    }

    focusBalance(nextRow, Math.sign(offset));
    return true;
  };

  const navigateRows = (event, row) => {
    const table = tableForRow(row);
    if (row.hidden) {
      return false;
    }

    if (event.key === "ArrowDown") {
      event.preventDefault();
      return moveSelection(row, 1);
    }

    if (event.key === "ArrowUp") {
      event.preventDefault();
      return moveSelection(row, -1);
    }

    if (event.key === "PageDown" || event.key === "PageUp") {
      event.preventDefault();
      const frame = gridFrame(row);
      const visibleCount = Math.max(Math.floor((frame.clientHeight - (table.tHead?.offsetHeight ?? 0)) / (row.offsetHeight || 27)) - 1, 1);
      return moveSelection(row, event.key === "PageDown" ? visibleCount : -visibleCount);
    }

    if (event.key === "Home") {
      event.preventDefault();
      focusBalance(table.tBodies[0]?.rows[0], -1);
      return true;
    }

    if (event.key === "End") {
      event.preventDefault();
      const currentRows = table.tBodies[0]?.rows;
      focusBalance(currentRows?.[currentRows.length - 1], 1);
      return true;
    }

    return false;
  };

  const sortValue = (row, key, type) => {
    if (type === "number") {
      return Number.parseFloat(row.dataset[dataKey(key)] ?? "0") || 0;
    }

    return normalize(row.dataset[dataKey(key)] ?? "");
  };

  const sortByHeader = (table, header) => {
    const key = header.dataset.sortKey;
    if (!key) {
      return;
    }

    const direction = header.dataset.sortDirection === "asc" ? "desc" : "asc";
    const selectedCode = selectedRow()?.dataset.code ?? "";
    const selectedType = selectedRow()?.dataset.cfType ?? "";
    const multiplier = direction === "desc" ? -1 : 1;
    const body = table.tBodies[0];

    tableRows(table)
      .sort((left, right) => {
        const leftValue = sortValue(left, key, header.dataset.sortType);
        const rightValue = sortValue(right, key, header.dataset.sortType);
        if (leftValue < rightValue) {
          return -1 * multiplier;
        }
        if (leftValue > rightValue) {
          return 1 * multiplier;
        }
        return 0;
      })
      .forEach((row) => body.appendChild(row));

    table.querySelectorAll("[data-sort-key]").forEach((candidate) => {
      const active = candidate === header;
      candidate.classList.toggle("is-sorted", active);
      candidate.dataset.sortDirection = active ? direction : "";
      candidate.setAttribute("aria-sort", active ? (direction === "asc" ? "ascending" : "descending") : "none");
    });

    selectRow(rows.find((row) => row.dataset.code === selectedCode && row.dataset.cfType === selectedType) ?? visibleRows(table)[0]);
  };

  const buildPayload = () => {
    payload.value = JSON.stringify(rows.map((row) => ({
      type: row.dataset.cfType ?? "",
      code: Number.parseInt(row.dataset.code ?? "0", 10),
      balance: parseDecimal(row.querySelector("[data-initial-cf-balance-input]")?.value)
    })));
  };

  const confirmExit = (url) => {
    if (!modified) {
      window.location.href = url;
      return;
    }

    window.SkyLabMessageBox?.show({
      title: "Saldo iniziale clienti e fornitori",
      message: "Uscire senza salvare le modifiche?",
      mode: "confirm",
      okText: "Esci",
      onConfirm: () => {
        window.location.href = url;
      }
    });
  };

  page.addEventListener("click", (event) => {
    const row = event.target instanceof Element ? event.target.closest("[data-initial-cf-row]") : null;
    if (row) {
      focusBalance(row);
    }
  });

  page.addEventListener("dblclick", (event) => {
    const row = event.target instanceof Element ? event.target.closest("[data-initial-cf-row]") : null;
    if (row) {
      focusBalance(row);
    }
  });

  page.addEventListener("focusin", (event) => {
    const input = event.target instanceof Element ? event.target.closest("[data-initial-cf-balance-input]") : null;
    const row = input?.closest("[data-initial-cf-row]");
    if (!input || !row) {
      return;
    }

    selectRow(row);
    input.value = formatForEdit(parseDecimal(input.value));
    input.select();
  });

  page.addEventListener("input", (event) => {
    const input = event.target instanceof Element ? event.target.closest("[data-initial-cf-balance-input]") : null;
    if (!input) {
      return;
    }

    cleanBalance(input);
    updateRowBalanceSort(input);
    modified = true;
  });

  page.addEventListener("focusout", (event) => {
    const input = event.target instanceof Element ? event.target.closest("[data-initial-cf-balance-input]") : null;
    if (input) {
      commitBalance(input);
    }
  });

  page.addEventListener("keydown", (event) => {
    const target = event.target instanceof Element ? event.target : null;
    const input = target?.closest("[data-initial-cf-balance-input]");
    const row = input?.closest("[data-initial-cf-row]") ?? target?.closest("[data-initial-cf-row]");
    if (!row) {
      return;
    }

    if (!input) {
      if (event.key === "Enter") {
        event.preventDefault();
        focusBalance(row);
      } else {
        navigateRows(event, row);
      }
      return;
    }

    if (event.key === "Enter") {
      event.preventDefault();
      event.stopPropagation();
      commitBalance(input);
      const currentRows = tableForRow(row).tBodies[0].rows;
      const next = currentRows[Math.min(row.sectionRowIndex + 1, currentRows.length - 1)];
      focusBalance(next, 1);
      return;
    }

    if (event.key === "Delete") {
      event.preventDefault();
      event.stopPropagation();
      input.value = "";
      updateRowBalanceSort(input);
      modified = true;
      return;
    }

    if (navigateRows(event, row)) {
      event.stopPropagation();
    }
  });

  tables.forEach((table) => {
    const headers = Array.from(table.querySelectorAll("[data-sort-key]"));
    headers.forEach((header) => {
      header.tabIndex = 0;
      header.setAttribute("role", "button");
      header.setAttribute("aria-sort", "none");
      header.addEventListener("click", () => sortByHeader(table, header));
      header.addEventListener("keydown", (event) => {
        if (event.key !== "Enter" && event.key !== " ") {
          return;
        }

        event.preventDefault();
        sortByHeader(table, header);
      });
    });

    const frame = table.closest(".initial-cf-grid-frame");
    let lastScrollTop = frame.scrollTop;
    let scrollFrame = 0;
    let wheelFrame = 0;

    frame.addEventListener("scroll", () => {
      if (scrollFrame) {
        window.cancelAnimationFrame(scrollFrame);
      }

      scrollFrame = window.requestAnimationFrame(() => {
        scrollFrame = 0;
        const currentScrollTop = frame.scrollTop;
        const delta = currentScrollTop - lastScrollTop;
        lastScrollTop = currentScrollTop;
        selectVisibleRowFromScroll(table, delta);
      });
    }, { passive: true });

    frame.addEventListener("wheel", (event) => {
      if (wheelFrame) {
        window.cancelAnimationFrame(wheelFrame);
      }

      const delta = event.deltaY;
      wheelFrame = window.requestAnimationFrame(() => {
        wheelFrame = window.requestAnimationFrame(() => {
          wheelFrame = 0;
          selectVisibleRowFromScroll(table, delta);
        });
      });
    }, { passive: true });
  });

  clearButton?.addEventListener("click", () => {
    rows.forEach((row) => {
      const input = row.querySelector("[data-initial-cf-balance-input]");
      if (input) {
        input.value = "";
        row.dataset.sortBalance = "0";
      }
    });
    totals.C = 0;
    totals.F = 0;
    updateTotals();
    modified = true;
    focusBalance(rows[0]);
  });

  yearSelect?.addEventListener("change", () => {
    const selectedYear = yearSelect.value;
    yearSelect.value = loadedYear;

    const loadSelectedYear = () => {
      window.location.href = `${window.location.pathname}?year=${encodeURIComponent(selectedYear)}`;
    };

    if (modified) {
      window.SkyLabMessageBox?.show({
        title: "Saldo iniziale clienti e fornitori",
        message: "Cambiare esercizio senza salvare le modifiche?",
        mode: "confirm",
        okText: "Cambia",
        onConfirm: loadSelectedYear
      });
      return;
    }

    loadSelectedYear();
  });

  form.addEventListener("submit", async (event) => {
    event.preventDefault();
    if (saveButton?.disabled) {
      return;
    }

    rows.forEach((row) => {
      const input = row.querySelector("[data-initial-cf-balance-input]");
      if (input) {
        commitBalance(input);
      }
    });
    buildPayload();
    modified = false;
    if (saveButton) {
      saveButton.disabled = true;
    }
    const progress = window.SkyProg;
    progress?.Show();

    let response;
    let responseHtml = "";
    let failure = "";
    try {
      response = await fetch(form.action || window.location.href, {
        method: "POST",
        body: new FormData(form),
        credentials: "same-origin",
        redirect: "follow"
      });
      responseHtml = await response.text();
      if (!response.ok) {
        failure = "Salvataggio non riuscito. Riprova.";
      }
    } catch {
      failure = "Impossibile completare il salvataggio. Verifica la connessione e riprova.";
    }

    const complete = () => {
      if (failure) {
        if (saveButton) {
          saveButton.disabled = false;
        }
        window.SkyLabMessageBox?.show({
          title: "Saldo iniziale clienti e fornitori",
          message: failure,
          variant: "error"
        });
        return;
      }

      document.open();
      document.write(responseHtml);
      document.close();
    };

    if (progress) {
      progress.Close(complete);
    } else {
      complete();
    }
  });

  exitLink?.addEventListener("click", (event) => {
    event.preventDefault();
    confirmExit(exitLink.href || "/");
  });

  document.addEventListener("keydown", (event) => {
    if (event.key !== "Escape" || document.body.classList.contains("lookup-open")) {
      return;
    }

    event.preventDefault();
    confirmExit("/");
  });

  updateTotals();
  if (rows.length > 0) {
    focusBalance(rows[0]);
  }
});

document.addEventListener("DOMContentLoaded", () => {
  const zoom = document.querySelector("[data-account-zoom]");
  const zoomGrid = zoom?.querySelector("[data-account-zoom-grid]");
  const zoomTable = zoom?.querySelector(".account-zoom-grid");
  const zoomBody = zoom?.querySelector(".account-zoom-grid tbody");
  const zoomRows = Array.from(zoom?.querySelectorAll("[data-zoom-row]") ?? []);
  const zoomSortHeaders = Array.from(zoom?.querySelectorAll("[data-account-zoom-sort]") ?? []);
  const zoomMaster = zoom?.querySelector("[data-account-zoom-master]");
  const zoomSearch = zoom?.querySelector("[data-account-zoom-search]");
  const zoomCount = zoom?.querySelector("[data-account-zoom-count]");
  const zoomEmpty = zoom?.querySelector("[data-account-zoom-empty]");
  const zoomOk = zoom?.querySelector("[data-account-zoom-ok]");
  const zoomCancel = zoom?.querySelector("[data-account-zoom-cancel]");
  const typeButtons = Array.from(zoom?.querySelectorAll("[data-account-type-filter]") ?? []);
  const rows = Array.from(document.querySelectorAll("[data-account-row]"));
  let activeRow = null;
  let activeType = "all";
  let zoomSort = { key: "masterCode", direction: "asc" };
  let zoomLastScrollTop = 0;
  let zoomScrollFrame = 0;
  let zoomWheelFrame = 0;

  if (!zoom || rows.length === 0) {
    return;
  }

  const normalize = (value) =>
    String(value ?? "")
      .normalize("NFD")
      .replace(/[\u0300-\u036f]/g, "")
      .toLowerCase();

  const updateRow = (row) => {
    const select = row.querySelector("[data-account-select]");
    const selectedCode = select?.value || "";
    const option = selectedCode && selectedCode !== "0"
      ? zoomRows.find((candidate) => candidate.dataset.accountCode === selectedCode)
      : null;
    const masterCode = row.querySelector("[data-master-code]");
    const masterDescription = row.querySelector("[data-master-description]");
    const accountCode = row.querySelector("[data-account-code]");
    const accountDescription = row.querySelector("[data-account-description]");

    if (!select || !option) {
      if (masterCode) masterCode.value = "";
      if (masterDescription) masterDescription.value = "";
      if (accountCode) accountCode.value = "";
      if (accountDescription) accountDescription.value = "";
      return;
    }

    if (masterCode) masterCode.value = String(option.dataset.masterCode || "").padStart(3, "0");
    if (masterDescription) masterDescription.value = option.dataset.masterDescription || "";
    if (accountCode) accountCode.value = String(option.dataset.accountCode || "").padStart(3, "0");
    if (accountDescription) accountDescription.value = option.dataset.accountDescription || "";
  };

  const selectedZoomRow = () => zoom.querySelector("[data-zoom-row].selected-row");
  const currentZoomRows = () => Array.from(zoomBody?.querySelectorAll("[data-zoom-row]") ?? zoomRows);
  const visibleZoomRows = () => currentZoomRows().filter((row) => !row.hidden);

  const sortValue = (row, key) => {
    const value = row.dataset[key] || "";
    if (key === "masterCode" || key === "accountCode") {
      return Number(value);
    }

    return normalize(value);
  };

  const updateZoomSortHeaders = () => {
    zoomSortHeaders.forEach((header) => {
      const active = header.dataset.accountZoomSort === zoomSort.key;
      header.classList.toggle("is-sorted", active);
      header.classList.toggle("is-descending", active && zoomSort.direction === "desc");
      header.setAttribute("aria-sort", active ? (zoomSort.direction === "asc" ? "ascending" : "descending") : "none");
    });
  };

  const applyZoomSort = () => {
    if (!zoomBody) {
      return;
    }

    const direction = zoomSort.direction === "asc" ? 1 : -1;
    [...zoomRows]
      .sort((left, right) => {
        const leftValue = sortValue(left, zoomSort.key);
        const rightValue = sortValue(right, zoomSort.key);

        if (leftValue < rightValue) return -1 * direction;
        if (leftValue > rightValue) return 1 * direction;
        return Number(left.dataset.accountCode || 0) - Number(right.dataset.accountCode || 0);
      })
      .forEach((row) => zoomBody.appendChild(row));

    updateZoomSortHeaders();
  };

  const typeMatches = (type) => {
    if (activeType === "all") return true;
    if (activeType === "equity") return type === "P" || type === "A" || type === "T";
    if (activeType === "costs") return type === "C";
    if (activeType === "revenues") return type === "R";
    return true;
  };

  const selectZoomRow = (row, focus = false, scroll = true) => {
    if (!row || row.hidden) {
      return;
    }

    zoomRows.forEach((candidate) => {
      candidate.classList.remove("selected-row");
      candidate.removeAttribute("aria-selected");
    });

    row.classList.add("selected-row");
    row.setAttribute("aria-selected", "true");

    if (focus) {
      row.focus({ preventScroll: true });
    }
    if (scroll) {
      const visibleRows = visibleZoomRows();
      if (zoomGrid && row === visibleRows[0]) {
        zoomGrid.scrollTop = 0;
      } else {
        row.scrollIntoView({ block: "nearest" });
      }
    }
  };

  const applyZoomFilters = () => {
    const master = zoomMaster?.value ?? "";
    const search = normalize(zoomSearch?.value);
    let visibleCount = 0;

    zoomRows.forEach((row) => {
      const masterOk = master === "" || row.dataset.masterCode === master;
      const textOk = search === "" || normalize(row.dataset.filterText).includes(search);
      const typeOk = typeMatches(row.dataset.accountType || "");
      row.hidden = !(masterOk && textOk && typeOk);
      if (!row.hidden) visibleCount += 1;
    });

    if (zoomCount) zoomCount.textContent = String(visibleCount);
    if (zoomEmpty) zoomEmpty.hidden = visibleCount !== 0;

    applyZoomSort();

    if (!selectedZoomRow() || selectedZoomRow().hidden) {
      selectZoomRow(visibleZoomRows()[0]);
    }
  };

  const chooseZoomRow = () => {
    const row = selectedZoomRow();
    const select = activeRow?.querySelector("[data-account-select]");
    if (!row || !select) {
      return;
    }

    const returnFocus = activeRow.querySelector("[data-account-add]");
    select.value = row.dataset.accountCode || "";
    updateRow(activeRow);
    closeZoom();
    returnFocus?.focus();
  };

  const openZoom = (row) => {
    activeRow = row;
    zoom.hidden = false;
    if (zoomSearch) zoomSearch.value = "";
    if (zoomMaster) zoomMaster.value = "";
    activeType = "all";
    typeButtons.forEach((button) => {
      button.classList.toggle("is-active", button.dataset.accountTypeFilter === "all");
    });
    applyZoomFilters();
    zoomLastScrollTop = zoomGrid?.scrollTop ?? 0;

    const currentCode = row.querySelector("[data-account-select]")?.value;
    const currentRow = currentCode
      ? zoomRows.find((candidate) => candidate.dataset.accountCode === currentCode)
      : null;
    selectZoomRow(currentRow || visibleZoomRows()[0]);
    zoomSearch?.focus();
  };

  function closeZoom() {
    zoom.hidden = true;
    activeRow = null;
  }

  const zoomViewport = () => {
    const gridRect = zoomGrid.getBoundingClientRect();
    const headerHeight = zoomTable?.tHead?.getBoundingClientRect().height ?? 0;

    return {
      top: gridRect.top + headerHeight,
      bottom: gridRect.bottom
    };
  };

  const isZoomRowVisible = (row, viewport) => {
    const rowRect = row.getBoundingClientRect();
    return rowRect.bottom > viewport.top + 1 && rowRect.top < viewport.bottom - 1;
  };

  const fullyVisibleZoomRows = (viewport) =>
    visibleZoomRows().filter((row) => {
      const rowRect = row.getBoundingClientRect();
      return rowRect.top >= viewport.top + 1 && rowRect.bottom <= viewport.bottom - 1;
    });

  const selectZoomRowFromScroll = (delta) => {
    if (!zoomGrid || zoom.hidden || delta === 0) {
      return;
    }

    const viewport = zoomViewport();
    const selected = selectedZoomRow();
    if (!selected || isZoomRowVisible(selected, viewport)) {
      return;
    }

    const currentRows = fullyVisibleZoomRows(viewport);
    if (currentRows.length === 0) {
      return;
    }

    selectZoomRow(delta > 0 ? currentRows[0] : currentRows[currentRows.length - 1], false, false);
  };

  rows.forEach((row) => {
    const select = row.querySelector("[data-account-select]");
    const clear = row.querySelector("[data-account-clear]");

    select?.addEventListener("change", () => updateRow(row));
    clear?.addEventListener("click", () => {
      if (!select) {
        return;
      }

      select.value = "0";
      updateRow(row);
      row.querySelector("[data-account-add]")?.focus();
    });

    updateRow(row);
  });

  document.addEventListener("click", (event) => {
    const add = event.target.closest("[data-account-add]");
    if (!add) {
      return;
    }

    const row = add.closest("[data-account-row]");
    if (!row) {
      return;
    }

    event.preventDefault();
    openZoom(row);
  });

  window.SkyAccountZoom = {
    openFromRow: openZoom,
    close: closeZoom,
    refreshRow: updateRow
  };

  zoomRows.forEach((row) => {
    row.addEventListener("click", () => selectZoomRow(row, true));
    row.addEventListener("dblclick", chooseZoomRow);
    row.addEventListener("keydown", (event) => {
      const currentRows = visibleZoomRows();
      const index = Math.max(currentRows.indexOf(row), 0);

      if (event.key === "Enter") {
        event.preventDefault();
        chooseZoomRow();
      } else if (event.key === "ArrowDown") {
        event.preventDefault();
        selectZoomRow(currentRows[Math.min(index + 1, currentRows.length - 1)], true);
      } else if (event.key === "ArrowUp") {
        event.preventDefault();
        selectZoomRow(currentRows[Math.max(index - 1, 0)], true);
      } else if (event.key === "Home") {
        event.preventDefault();
        selectZoomRow(currentRows[0], true);
      } else if (event.key === "End") {
        event.preventDefault();
        selectZoomRow(currentRows[currentRows.length - 1], true);
      }
    });
  });

  zoomSortHeaders.forEach((header) => {
    const sort = () => {
      const key = header.dataset.accountZoomSort;
      if (!key) {
        return;
      }

      zoomSort = {
        key,
        direction: zoomSort.key === key && zoomSort.direction === "asc" ? "desc" : "asc",
      };
      applyZoomSort();
      selectZoomRow(visibleZoomRows()[0], true);
    };

    header.addEventListener("click", sort);
    header.addEventListener("keydown", (event) => {
      if (event.key !== "Enter" && event.key !== " ") {
        return;
      }

      event.preventDefault();
      sort();
    });
  });

  typeButtons.forEach((button) => {
    button.addEventListener("click", () => {
      activeType = button.dataset.accountTypeFilter || "all";
      typeButtons.forEach((candidate) => candidate.classList.toggle("is-active", candidate === button));
      if (zoomMaster) zoomMaster.value = "";
      if (zoomSearch) zoomSearch.value = "";
      applyZoomFilters();
    });
  });

  zoomMaster?.addEventListener("change", applyZoomFilters);
  zoomSearch?.addEventListener("input", applyZoomFilters);
  zoomOk?.addEventListener("click", chooseZoomRow);
  zoomCancel?.addEventListener("click", closeZoom);
  zoomGrid?.addEventListener("scroll", () => {
    if (zoomScrollFrame) {
      window.cancelAnimationFrame(zoomScrollFrame);
    }

    zoomScrollFrame = window.requestAnimationFrame(() => {
      zoomScrollFrame = 0;
      const currentScrollTop = zoomGrid.scrollTop;
      const delta = currentScrollTop - zoomLastScrollTop;
      zoomLastScrollTop = currentScrollTop;
      selectZoomRowFromScroll(delta);
    });
  }, { passive: true });
  zoomGrid?.addEventListener("wheel", (event) => {
    if (zoomWheelFrame) {
      window.cancelAnimationFrame(zoomWheelFrame);
    }

    const delta = event.deltaY;
    zoomWheelFrame = window.requestAnimationFrame(() => {
      zoomWheelFrame = window.requestAnimationFrame(() => {
        zoomWheelFrame = 0;
        selectZoomRowFromScroll(delta);
      });
    });
  }, { passive: true });

  zoom.addEventListener("keydown", (event) => {
    if (event.key === "Escape") {
      event.preventDefault();
      closeZoom();
    }
  });

  applyZoomSort();
});

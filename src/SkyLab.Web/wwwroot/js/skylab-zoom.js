(() => {
  const cache = new Map();
  let overlay;
  let title;
  let search;
  let count;
  let table;
  let body;
  let frame;
  let activeRows = [];
  let visibleRows = [];
  let selectedIndex = -1;
  let sortState = { key: "", direction: "asc" };
  let complete = null;
  let opener = null;
  let lastScrollTop = 0;
  let scrollFrame = 0;
  let wheelFrame = 0;

  const ensureDialog = () => {
    if (overlay) return;
    overlay = document.createElement("dialog");
    overlay.className = "customer-lookup-dialog skylab-zoom-dialog";
    overlay.innerHTML = `
        <div class="customer-lookup-title"><h2 id="skylab-zoom-title"></h2></div>
        <div class="customer-lookup-grid skylab-zoom-frame" tabindex="0">
          <table class="skylab-zoom-grid"><thead><tr></tr></thead><tbody></tbody></table>
          <div class="skylab-zoom-empty" hidden>Nessun elemento trovato.</div>
        </div>
        <div class="customer-lookup-footer skylab-zoom-tools">
          <label for="skylab-zoom-search">Cerca</label>
          <input id="skylab-zoom-search" type="search" autocomplete="off" />
          <div class="customer-lookup-records-label">Records</div>
          <div id="skylab-zoom-count" class="customer-lookup-records-count">0</div>
          <div></div>
          <div class="customer-lookup-actions"><button class="btn btn-primary" type="button" data-skylab-zoom-ok>OK</button><button class="btn customer-menu-button" type="button" data-skylab-zoom-cancel>Annulla</button></div>
        </div>`;
    document.body.appendChild(overlay);
    title = overlay.querySelector("#skylab-zoom-title");
    search = overlay.querySelector("#skylab-zoom-search");
    count = overlay.querySelector("#skylab-zoom-count");
    table = overlay.querySelector(".skylab-zoom-grid");
    body = table.tBodies[0];
    frame = overlay.querySelector(".skylab-zoom-frame");
    search.addEventListener("input", applyFilter);
    overlay.querySelector("[data-skylab-zoom-ok]").addEventListener("click", confirm);
    overlay.querySelector("[data-skylab-zoom-cancel]").addEventListener("click", cancel);
    overlay.addEventListener("cancel", event => { event.preventDefault(); cancel(); });
    overlay.addEventListener("keydown", event => {
      const action = {
        Escape: cancel,
        ArrowDown: () => move(1),
        ArrowUp: () => move(-1),
        Home: () => choose(0),
        End: () => choose(visibleRows.length - 1),
        Enter: confirm
      }[event.key];
      if (!action) return;
      event.preventDefault();
      event.stopPropagation();
      action();
    });
    frame.addEventListener("scroll", () => {
      cancelAnimationFrame(scrollFrame);
      scrollFrame = requestAnimationFrame(() => {
        const delta = frame.scrollTop - lastScrollTop;
        lastScrollTop = frame.scrollTop;
        selectFromScroll(delta);
      });
    });
    frame.addEventListener("wheel", event => {
      cancelAnimationFrame(wheelFrame);
      wheelFrame = requestAnimationFrame(() => requestAnimationFrame(() => selectFromScroll(event.deltaY)));
    }, { passive: true });
  };

  const load = async catalog => {
    if (!cache.has(catalog)) {
      cache.set(catalog, fetch(`/api/zoom/${encodeURIComponent(catalog)}`, { headers: { Accept: "application/json" } })
        .then(response => {
          if (!response.ok) throw new Error(`Catalogo Zoom non disponibile: ${catalog}`);
          return response.json();
        }));
    }
    return cache.get(catalog);
  };

  const format = (value, column) => {
    if (value === null || value === undefined) return "";
    if (column.type === "number") return Number(value).toLocaleString("it-IT", { minimumFractionDigits: 2, maximumFractionDigits: 3 });
    return String(value);
  };

  const render = payload => {
    title.textContent = payload.title || "Selezione";
    const header = table.tHead.rows[0];
    header.replaceChildren();
    body.replaceChildren();
    payload.columns.forEach(column => {
      const cell = document.createElement("th");
      const button = document.createElement("button");
      button.type = "button";
      button.textContent = column.label;
      button.dataset.zoomSort = column.key;
      button.dataset.zoomType = column.type || "text";
      cell.setAttribute("aria-sort", "none");
      if (column.width) cell.style.width = column.width;
      if (column.align) cell.style.textAlign = column.align;
      button.addEventListener("click", () => sort(column.key, column.type));
      cell.appendChild(button);
      header.appendChild(cell);
    });
    activeRows = (payload.rows || []).map(item => {
      const row = document.createElement("tr");
      row.tabIndex = -1;
      row._zoomValue = item;
      row._zoomSearch = Object.values(item).join(" ").toLocaleLowerCase("it");
      payload.columns.forEach(column => {
        const cell = document.createElement("td");
        cell.textContent = format(item[column.key], column);
        if (column.align) cell.style.textAlign = column.align;
        row.appendChild(cell);
      });
      row.addEventListener("click", () => {
        selectRow(row);
        frame.focus({ preventScroll: true });
      });
      row.addEventListener("dblclick", confirm);
      return row;
    });
    sortState = { key: payload.defaultSort || payload.columns[0]?.key || "", direction: "asc" };
    sort(sortState.key, payload.columns.find(column => column.key === sortState.key)?.type, true);
  };

  const sort = (key, type = "text", initial = false) => {
    if (!initial) sortState.direction = sortState.key === key && sortState.direction === "asc" ? "desc" : "asc";
    sortState.key = key;
    const direction = sortState.direction === "asc" ? 1 : -1;
    activeRows.sort((left, right) => {
      const a = left._zoomValue[key] ?? "";
      const b = right._zoomValue[key] ?? "";
      return (type === "number" ? Number(a) - Number(b) : String(a).localeCompare(String(b), "it", { numeric: true, sensitivity: "base" })) * direction;
    });
    activeRows.forEach(row => body.appendChild(row));
    table.querySelectorAll("[data-zoom-sort]").forEach(button => {
      const active = button.dataset.zoomSort === key;
      button.classList.toggle("is-active", active);
      button.dataset.direction = active ? sortState.direction : "";
      button.closest("th")?.setAttribute("aria-sort", active ? (sortState.direction === "asc" ? "ascending" : "descending") : "none");
    });
    applyFilter();
  };

  const selectRow = row => {
    const index = visibleRows.indexOf(row);
    if (index < 0) return;
    choose(index);
  };
  const ensureVisible = row => {
    const rowRect = row.getBoundingClientRect();
    const frameRect = frame.getBoundingClientRect();
    const headerBottom = table.tHead.getBoundingClientRect().bottom;
    if (rowRect.top < headerBottom) frame.scrollTop -= headerBottom - rowRect.top;
    else if (rowRect.bottom > frameRect.bottom) frame.scrollTop += rowRect.bottom - frameRect.bottom;
  };
  const choose = index => {
    if (!visibleRows.length) { selectedIndex = -1; return; }
    selectedIndex = Math.max(0, Math.min(index, visibleRows.length - 1));
    activeRows.forEach(row => row.classList.remove("selected"));
    const row = visibleRows[selectedIndex];
    row.classList.add("selected");
    ensureVisible(row);
  };
  const move = direction => choose(selectedIndex + direction);
  const isVisibleInFrame = row => {
    const rowRect = row.getBoundingClientRect();
    const frameRect = frame.getBoundingClientRect();
    const headerBottom = table.tHead.getBoundingClientRect().bottom;
    return rowRect.bottom > headerBottom && rowRect.top < frameRect.bottom;
  };
  const selectFromScroll = direction => {
    if (!direction || !visibleRows.length) return;
    const selected = visibleRows[selectedIndex];
    if (selected && isVisibleInFrame(selected)) return;
    const rowsInView = visibleRows.filter(isVisibleInFrame);
    const row = direction > 0 ? rowsInView[0] : rowsInView[rowsInView.length - 1];
    if (row) selectRow(row);
  };
  const applyFilter = () => {
    const value = (search?.value || "").trim().toLocaleLowerCase("it");
    visibleRows = activeRows.filter(row => {
      const visible = !value || row._zoomSearch.includes(value);
      row.hidden = !visible;
      return visible;
    });
    count.textContent = String(visibleRows.length);
    overlay.querySelector(".skylab-zoom-empty").hidden = visibleRows.length !== 0;
    choose(0);
  };
  const close = value => {
    if (overlay.open) overlay.close();
    document.body.classList.remove("lookup-open");
    const resolve = complete;
    complete = null;
    resolve?.(value);
    opener?.focus?.();
  };
  const confirm = () => close(visibleRows[selectedIndex]?._zoomValue || null);
  const cancel = () => close(null);

  const open = async (catalog, options = {}) => {
    ensureDialog();
    if (complete) close(null);
    opener = options.opener || document.activeElement;
    try {
      const payload = await load(catalog);
      render(payload);
      search.value = "";
      overlay.showModal();
      document.body.classList.add("lookup-open");
      applyFilter();
      const current = options.current;
      if (current !== undefined && current !== null && current !== "") {
        const row = visibleRows.find(candidate => String(candidate._zoomValue.code ?? candidate._zoomValue.id ?? "") === String(current));
        if (row) selectRow(row);
      }
      frame.scrollTop = 0;
      lastScrollTop = 0;
      search.focus();
      return await new Promise(resolve => { complete = resolve; });
    } catch (error) {
      cache.delete(catalog);
      window.SkyLabMessageBox?.show?.({ title: "SkyLab - Zoom", message: "Impossibile caricare lo Zoom richiesto.", detail: error.message });
      return null;
    }
  };

  const applyResult = (button, result) => {
    if (!result) return;
    Object.entries(result).forEach(([key, value]) => {
      const dataKey = `skylabZoomTarget${key[0].toUpperCase()}${key.slice(1)}`;
      const selector = button.dataset[dataKey];
      if (!selector) return;
      const target = document.querySelector(selector);
      if (!target) return;
      target.value = value ?? "";
      target.dispatchEvent(new Event("input", { bubbles: true }));
      target.dispatchEvent(new Event("change", { bubbles: true }));
    });
    button.dispatchEvent(new CustomEvent("skylab:zoom-selected", { bubbles: true, detail: result }));
  };

  document.addEventListener("click", async event => {
    const button = event.target.closest("[data-skylab-zoom]");
    if (!button) return;
    event.preventDefault();
    const catalog = button.dataset.skylabZoom;
    const currentSelector = button.dataset.skylabZoomTargetCode || button.dataset.skylabZoomTargetId;
    const result = await open(catalog, { opener: button, current: currentSelector ? document.querySelector(currentSelector)?.value : "" });
    applyResult(button, result);
  });

  const find = async (catalog, key, value) => {
    const payload = await load(catalog);
    const rows = Array.isArray(payload) ? payload : (payload.rows ?? payload.items ?? payload.data ?? []);
    const expected = String(value ?? "").trim().toLocaleUpperCase("it");
    if (!expected) return null;
    return rows.find(row => String(row?.[key] ?? "").trim().toLocaleUpperCase("it") === expected) ?? null;
  };

  window.SkyLabZoom = Object.freeze({ open, find, clearCache: catalog => catalog ? cache.delete(catalog) : cache.clear() });
})();

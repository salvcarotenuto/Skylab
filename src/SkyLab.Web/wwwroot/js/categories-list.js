(() => {
  const page = document.querySelector("[data-category-page]");
  if (!page) return;
  const rows = [...page.querySelectorAll("[data-category-row]")];
  const search = page.querySelector("[data-category-search]");
  const clear = page.querySelector("[data-category-clear]");
  const count = page.querySelector("[data-category-count]");
  const empty = page.querySelector("[data-category-empty]");
  let selected = null;

  const visibleRows = () => rows.filter(row => !row.hidden);
  const choose = (row, focus = false) => {
    rows.forEach(item => item.classList.toggle("selected", item === row));
    selected = row;
    if (row && focus) { row.focus({preventScroll:true}); row.scrollIntoView({block:"nearest"}); }
  };
  const filter = () => {
    const query = search.value.trim().toLocaleLowerCase("it");
    let visible = 0;
    rows.forEach(row => {
      row.hidden = !!query && !`${row.dataset.code} ${row.dataset.description}`.toLocaleLowerCase("it").includes(query);
      if (!row.hidden) visible++;
    });
    count.textContent = visible;
    clear.hidden = !search.value;
    empty.hidden = visible > 0;
    if (!selected || selected.hidden) choose(visibleRows()[0] || null);
  };
  const move = (row, offset) => {
    const visible = visibleRows();
    const index = Math.max(0, visible.indexOf(row));
    choose(visible[Math.max(0, Math.min(visible.length - 1, index + offset))], true);
  };
  rows.forEach(row => {
    row.addEventListener("click", () => choose(row));
    row.addEventListener("keydown", event => {
      if (event.key === "ArrowDown" || event.key === "ArrowUp") { event.preventDefault(); move(row, event.key === "ArrowDown" ? 1 : -1); }
      else if (event.key === "Home" || event.key === "End") { event.preventDefault(); const visible=visibleRows(); choose(event.key === "Home" ? visible[0] : visible.at(-1), true); }
    });
  });
  search.addEventListener("input", filter);
  clear.addEventListener("click", () => { search.value=""; filter(); search.focus(); });
  document.addEventListener("keydown", event => {
    const target=event.target;
    const editable=target instanceof HTMLInputElement||target instanceof HTMLSelectElement||target instanceof HTMLTextAreaElement||target instanceof HTMLButtonElement||target?.isContentEditable;
    if(event.defaultPrevented||editable||event.altKey||event.ctrlKey||event.metaKey)return;
    if(event.key.length===1){event.preventDefault();search.value+=event.key;search.focus();filter();}
    else if(event.key==="Backspace"&&search.value){event.preventDefault();search.value=search.value.slice(0,-1);search.focus();filter();}
  });
  page.querySelectorAll("[data-category-sort]").forEach(header => header.addEventListener("click", () => {
    const key=header.dataset.categorySort, body=header.closest("table").tBodies[0];
    rows.sort((a,b)=>key==="code"?Number(a.dataset.code)-Number(b.dataset.code):a.dataset.description.localeCompare(b.dataset.description,"it",{sensitivity:"base"})).forEach(row=>body.append(row));
  }));
  filter(); choose(visibleRows()[0] || null);
})();

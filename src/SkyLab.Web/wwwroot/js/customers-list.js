document.addEventListener("DOMContentLoaded", () => {
  const page = document.querySelector("[data-customer-list-page]");
  const search = page?.querySelector("[data-customer-search]");
  const inactive = page?.querySelector("[data-customer-inactive]");
  const form = page?.querySelector("[data-customer-search-form]");
  const rows = Array.from(page?.querySelectorAll("[data-customer-row]") ?? []);
  const count = page?.querySelector("[data-customer-count]");
  const empty = page?.querySelector("[data-customer-empty]");

  if (!page || !search) return;

  const normalize = (value) => String(value ?? "")
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .toLocaleLowerCase("it-IT")
    .trim();

  const applyFilter = () => {
    const query = normalize(search.value);
    let visible = 0;

    rows.forEach((row) => {
      const name = normalize(row.dataset.filterName);
      const code = normalize(row.dataset.filterCode);
      const matches = query === "" || name.includes(query) || code.includes(query);
      row.hidden = !matches;
      if (matches) visible += 1;
    });

    if (count) count.textContent = String(visible);
    if (empty) empty.hidden = visible !== 0;
  };

  search.addEventListener("input", applyFilter);
  inactive?.addEventListener("change", () => form?.requestSubmit());

  document.addEventListener("keydown", (event) => {
    const target = event.target;
    const isEditable = target instanceof HTMLInputElement ||
      target instanceof HTMLTextAreaElement ||
      target instanceof HTMLSelectElement ||
      target instanceof HTMLButtonElement ||
      target?.isContentEditable;

    if (event.defaultPrevented || isEditable || event.altKey || event.ctrlKey || event.metaKey) return;

    if (event.key.length === 1) {
      event.preventDefault();
      search.value += event.key;
      search.focus({ preventScroll: true });
      applyFilter();
      return;
    }

    if (event.key === "Backspace" && search.value !== "") {
      event.preventDefault();
      search.value = search.value.slice(0, -1);
      search.focus({ preventScroll: true });
      applyFilter();
    }
  });

  applyFilter();
});

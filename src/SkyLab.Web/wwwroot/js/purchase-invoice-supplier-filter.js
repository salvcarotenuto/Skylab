document.addEventListener("DOMContentLoaded", () => {
  const filterForm = document.querySelector("[data-purchase-invoice-filters]");
  const hiddenCode = document.getElementById("supplierCode");
  const displayCode = document.getElementById("supplierCodeDisplay");
  const displayName = document.getElementById("supplierNameDisplay");
  const dialog = document.querySelector('[data-party-lookup="F"]');

  if (!filterForm || !hiddenCode || !displayCode || !displayName || !dialog) {
    return;
  }

  const source = JSON.parse(dialog.querySelector("[data-party-source]")?.textContent || "[]");
  const findSupplier = () => {
    const code = Number(String(displayCode.value || "").replace(/\D/g, ""));
    return source.find((row) => Number(row.code) === code);
  };

  const applySupplier = (code, name, submit = true) => {
    hiddenCode.value = code ? String(Number(code)) : "";
    displayCode.value = code ? String(Number(code)).padStart(5, "0") : "";
    displayName.value = name || "";

    if (submit) {
      hiddenCode.dispatchEvent(new Event("change", { bubbles: true }));
    }
  };

  displayCode.addEventListener("input", () => {
    displayCode.value = displayCode.value.replace(/\D/g, "");
  });

  displayCode.addEventListener("change", () => {
    const supplier = findSupplier();
    applySupplier(supplier?.code || "", supplier?.name || "", true);
  });

  displayCode.addEventListener("keydown", (event) => {
    if (event.key === "Enter") {
      event.preventDefault();
      displayCode.blur();
    }
  });

  dialog.addEventListener("skylab:party-selected", (event) => {
    if (event.detail.target && event.detail.target !== "purchase-invoices") {
      return;
    }

    applySupplier(event.detail.code, event.detail.name, true);
  });
});

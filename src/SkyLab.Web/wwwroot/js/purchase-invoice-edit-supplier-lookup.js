document.addEventListener("DOMContentLoaded", () => {
  const dialog = document.querySelector('[data-party-lookup="F"]');
  const code = document.getElementById("purchaseInvoiceSupplierCode");
  const codeDisplay = document.getElementById("purchaseInvoiceSupplierCodeDisplay");
  const name = document.getElementById("purchaseInvoiceSupplierName");

  if (!dialog || !code || !codeDisplay || !name) {
    return;
  }

  const source = JSON.parse(dialog.querySelector("[data-party-source]")?.textContent || "[]");
  const findSupplier = () => {
    const value = Number(String(codeDisplay.value || "").replace(/\D/g, ""));
    return source.find((row) => Number(row.code) === value);
  };

  const applySupplier = (supplier) => {
    code.value = supplier ? String(Number(supplier.code)) : "";
    codeDisplay.value = supplier ? String(Number(supplier.code)).padStart(5, "0") : "";
    name.value = supplier?.name || "";
    code.dispatchEvent(new Event("change", { bubbles: true }));
    codeDisplay.dispatchEvent(new Event("change", { bubbles: true }));
  };

  codeDisplay.addEventListener("input", () => {
    codeDisplay.value = codeDisplay.value.replace(/\D/g, "");
  });

  codeDisplay.addEventListener("change", () => {
    applySupplier(findSupplier());
  });

  codeDisplay.addEventListener("keydown", (event) => {
    if (event.key === "Enter") {
      event.preventDefault();
      codeDisplay.blur();
    }
  });

  dialog.addEventListener("skylab:party-selected", (event) => {
    if (event.detail.target && event.detail.target !== "purchase-invoice-edit") {
      return;
    }

    applySupplier(event.detail);
    codeDisplay.focus();
  });
});

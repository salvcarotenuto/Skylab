document.addEventListener("DOMContentLoaded", () => {
  const form = document.querySelector("[data-sales-invoice-filters]");
  const codeInput = document.getElementById("sales-invoice-customer");
  const nameInput = document.getElementById("sales-invoice-customer-name");
  const dialog = document.querySelector('[data-party-lookup="C"]');

  if (!form || !codeInput || !nameInput || !dialog) return;

  const source = JSON.parse(dialog.querySelector("[data-party-source]")?.textContent || "[]");
  const customerByCode = code => source.find(item => Number(item.code) === Number(code));

  const applyCustomer = (customer, submit) => {
    codeInput.value = customer ? String(Number(customer.code)).padStart(5, "0") : "";
    nameInput.value = customer?.name || "";
    if (submit) form.requestSubmit();
  };

  const acceptTypedCode = () => {
    const value = String(codeInput.value || "").replace(/\D/g, "").slice(0, 5);
    if (!value) {
      applyCustomer(null, true);
      return;
    }
    const customer = customerByCode(value);
    if (customer) {
      applyCustomer(customer, true);
      return;
    }

    const formattedCode = value.padStart(5, "0");
    codeInput.value = formattedCode;
    nameInput.value = "";
    window.SkyLabMessageBox?.show?.({
      title: "Fatture di vendita",
      message: `Il cliente ${formattedCode} non è presente in anagrafica.`,
      variant: "error",
      okText: "OK",
      onConfirm: () => {
        codeInput.focus();
      }
    });
  };

  codeInput.addEventListener("input", () => {
    codeInput.value = codeInput.value.replace(/\D/g, "").slice(0, 5);
    nameInput.value = "";
  });

  codeInput.addEventListener("change", acceptTypedCode);
  codeInput.addEventListener("keydown", event => {
    if (event.key !== "Enter") return;
    event.preventDefault();
    acceptTypedCode();
  });

  dialog.addEventListener("skylab:party-selected", event => {
    if (event.detail.target && event.detail.target !== "sales-invoices") return;
    applyCustomer(event.detail, true);
  });
});

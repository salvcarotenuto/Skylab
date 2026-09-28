document.addEventListener("DOMContentLoaded", () => {
  const codeInput = document.getElementById("sales-invoice-edit-customer");
  const nameInput = document.getElementById("sales-invoice-edit-customer-name");
  const dialog = document.querySelector('[data-party-lookup="C"]');
  if (!codeInput || !nameInput || !dialog) return;

  const source = JSON.parse(dialog.querySelector("[data-party-source]")?.textContent || "[]");
  const customerByCode = code => source.find(customer => Number(customer.code) === Number(code));
  const formatCode = value => String(value || "").replace(/\D/g, "").slice(0, 5).padStart(5, "0");

  const applyCustomer = customer => {
    codeInput.value = customer ? String(Number(customer.code)).padStart(5, "0") : "";
    nameInput.value = customer?.name || "";
  };

  const showMissingCustomer = formattedCode => {
    nameInput.value = "";
    window.SkyLabMessageBox?.show?.({
      title: "Fattura di vendita",
      message: `Il cliente ${formattedCode} non è presente in anagrafica.`,
      variant: "error",
      okText: "OK",
      onConfirm: () => codeInput.focus()
    });
  };

  const resolveTypedCode = () => {
    const digits = codeInput.value.replace(/\D/g, "").slice(0, 5);
    if (!digits) {
      applyCustomer(null);
      return true;
    }

    const formattedCode = formatCode(digits);
    codeInput.value = formattedCode;
    const customer = customerByCode(formattedCode);
    if (!customer) {
      showMissingCustomer(formattedCode);
      return false;
    }

    applyCustomer(customer);
    return true;
  };

  codeInput.addEventListener("input", () => {
    codeInput.value = codeInput.value.replace(/\D/g, "").slice(0, 5);
    nameInput.value = "";
  });

  codeInput.addEventListener("change", resolveTypedCode);
  codeInput.addEventListener("keydown", event => {
    if (event.key !== "Enter") return;
    if (resolveTypedCode()) return;
    event.preventDefault();
    event.stopPropagation();
  });

  dialog.addEventListener("skylab:party-selected", event => {
    if (event.detail.target !== "sales-invoice-edit") return;
    applyCustomer(event.detail);
  });
});

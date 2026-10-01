(() => {
    const button = document.querySelector("[data-sales-invoice-mark-rejected]");
    if (!button) return;

    const showMessage = (message, variant = "error") => {
        window.SkyLabMessageBox?.show({ title: "Fattura di vendita", message, variant });
    };

    button.addEventListener("click", async () => {
        const row = document.querySelector(".sales-invoices-grid tbody tr.selected, .sales-invoices-grid tbody tr.selected-row");
        const invoiceId = row?.dataset.invoiceId;
        if (!invoiceId) {
            showMessage("Selezionare la fattura da contrassegnare come rifiutata dallo SdI.");
            return;
        }

        window.SkyLabMessageBox?.show({
            mode: "confirm",
            variant: "confirm",
            title: "Rifiuto SDI",
            message: "Confermi di aver ricevuto dallo SdI la notifica di rifiuto? Solo dopo questa conferma sarà possibile un nuovo invio PEC.",
            okText: "Conferma",
            onConfirm: async () => {
                button.disabled = true;
                try {
                    const response = await fetch(`/api/sales-invoices/${encodeURIComponent(invoiceId)}/electronic-rejection`, {
                        method: "POST",
                        credentials: "same-origin",
                        headers: { Accept: "application/json" }
                    });
                    const result = await response.json().catch(() => null);
                    if (!response.ok || !result?.success) {
                        showMessage(result?.message || "Impossibile registrare il rifiuto SdI.");
                        return;
                    }
                    window.location.reload();
                } catch {
                    showMessage("Impossibile completare la registrazione del rifiuto SdI.");
                } finally {
                    button.disabled = false;
                }
            }
    });
})();

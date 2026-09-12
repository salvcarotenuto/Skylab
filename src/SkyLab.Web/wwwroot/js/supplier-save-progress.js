(() => {
  const form = document.getElementById("supplier-form");
  const button = document.querySelector("[data-supplier-save]");
  const cancelButton = document.querySelector("[data-supplier-cancel]");
  if (!form || !button) {
    return;
  }

  const isExternalPurchaseInvoiceCall = () =>
    form.querySelector('[name="azione"]')?.value === "102"
    && form.querySelector('[name="returnTo"]')?.value === "purchaseInvoiceXml";

  const notifyCancel = () => {
    window.parent?.postMessage({
      type: "skylab:supplier-cancel"
    }, window.location.origin);
  };

  const showError = (message) => {
    if (window.SkyLabMessageBox?.show) {
      window.SkyLabMessageBox.show({
        title: "Fornitore",
        message,
        variant: "error"
      });
      return;
    }

    window.alert(message);
  };

  form.addEventListener("submit", async (event) => {
    event.preventDefault();
    if (!form.checkValidity()) {
      const invalid = form.querySelector(":invalid");
      const message = invalid?.name === "Fornitore.Name"
        ? "Campo Nome obbligatorio."
        : invalid?.name === "Fornitore.AccountCode"
          ? "Campo Contropartita obbligatorio."
          : invalid?.validationMessage || "Dati fornitore non validi.";
      showError(message);
      invalid?.focus();
      return;
    }

    button.disabled = true;
    window.SkyProg?.Show();
    let response;
    let html = "";
    let payload = null;
    let error = "";

    try {
      response = await fetch(form.action || location.href, {
        method: "POST",
        body: new FormData(form),
        credentials: "same-origin",
        redirect: "follow",
        headers: {
          "X-Requested-With": "XMLHttpRequest",
          Accept: "application/json, text/html"
        }
      });

      const contentType = response.headers.get("content-type") || "";
      if (contentType.includes("application/json")) {
        payload = await response.json();
      } else {
        html = await response.text();
      }

      if (!response.ok || payload?.success === false) {
        error = payload?.message || "Salvataggio non riuscito.";
      }
    } catch {
      error = "Impossibile completare il salvataggio.";
    } finally {
      window.SkyProg?.Close(() => {
        button.disabled = false;
        if (error) {
          showError(error);
          return;
        }

        if (payload?.success) {
          if (isExternalPurchaseInvoiceCall()) {
            window.parent?.postMessage({
              type: "skylab:supplier-saved",
              code: payload.code,
              name: payload.name
            }, window.location.origin);
            return;
          }

          if (payload.redirectUrl) {
            location.href = payload.redirectUrl;
            return;
          }

          location.href = "/Fornitori";
          return;
        }

        if (response?.redirected) {
          location.href = response.url;
          return;
        }

        document.open();
        document.write(html || "");
        document.close();
      });
    }
  });

  cancelButton?.addEventListener("click", () => {
    if (isExternalPurchaseInvoiceCall()) {
      notifyCancel();
      return;
    }

    window.location.href = "/Fornitori";
  });

  document.addEventListener("click", (event) => {
    const link = event.target?.closest?.('a[href*="/Fornitori"]');
    if (!link || !isExternalPurchaseInvoiceCall()) {
      return;
    }

    event.preventDefault();
    notifyCancel();
  }, true);
})();

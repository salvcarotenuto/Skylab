document.addEventListener("DOMContentLoaded", () => {
  const form = document.querySelector("#work-form");
  const saveButton = document.querySelector("[data-work-save]");
  if (!form || !saveButton) return;

  let saving = false;
  const showError = message => {
    if (window.MicronoteMessageBox?.show) {
      window.MicronoteMessageBox.show({ title: "Scheda lavoro", message });
    } else {
      window.alert(message);
    }
  };

  form.addEventListener("submit", async event => {
    const submitter = event.submitter;
    if (submitter && submitter !== saveButton) return;
    if (!form.checkValidity()) return;
    if (window.jQuery?.fn?.valid && !window.jQuery(form).valid()) return;

    event.preventDefault();
    if (saving) return;
    saving = true;
    saveButton.disabled = true;
    window.SkyProg?.Show();

    let destination = "";
    let returnedPage = "";
    let errorMessage = "";
    try {
      const response = await fetch(form.action || location.href, {
        method: "POST",
        body: new FormData(form),
        credentials: "same-origin",
        headers: { "X-Requested-With": "XMLHttpRequest" }
      });
      if (!response.ok) throw new Error("Il salvataggio della scheda non è riuscito.");
      if (response.redirected) destination = response.url;
      else returnedPage = await response.text();
    } catch (error) {
      errorMessage = error instanceof Error ? error.message : "Il salvataggio della scheda non è riuscito.";
    } finally {
      const finish = () => {
        saving = false;
        if (saveButton.isConnected) saveButton.disabled = false;
        if (destination) {
          location.href = destination;
        } else if (returnedPage) {
          document.open();
          document.write(returnedPage);
          document.close();
        } else if (errorMessage) {
          showError(errorMessage);
        }
      };
      if (window.SkyProg) window.SkyProg.Close(finish);
      else finish();
    }
  });
});

(() => {
  const form = document.getElementById("customer-form");
  const button = document.querySelector("[data-customer-save]");
  if (!form || !button) return;

  form.addEventListener("submit", async event => {
    if (event.defaultPrevented) return;
    event.preventDefault();
    if (!form.reportValidity()) return;

    const progress = window.parent !== window && window.parent.SkyProg
      ? window.parent.SkyProg
      : window.SkyProg;
    button.disabled = true;
    progress?.Show();

    let response;
    let html = "";
    let error = "";
    try {
      response = await fetch(form.action || location.href, {
        method: "POST",
        body: new FormData(form),
        credentials: "same-origin",
        redirect: "follow"
      });
      html = await response.text();
      if (!response.ok) error = "Salvataggio non riuscito.";
    } catch {
      error = "Impossibile completare il salvataggio.";
    } finally {
      progress?.Close(() => {
        button.disabled = false;
        if (error) {
          window.SkyLabMessageBox?.show({ title: "SkyLab - attenzione", message: error, variant: "error" });
          return;
        }
        if (response?.redirected) {
          location.href = response.url;
          return;
        }
        document.open();
        document.write(html);
        document.close();
      });
    }
  });
})();

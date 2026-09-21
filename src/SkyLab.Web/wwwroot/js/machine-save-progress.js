(() => {
  "use strict";

  document.addEventListener("DOMContentLoaded", () => {
    const form = document.querySelector("#machine-form");
    const save = document.querySelector(".machine-save-button");
    if (!form || !save) return;

    form.addEventListener("submit", async (event) => {
      const submitter = event.submitter;
      if (submitter && submitter !== save) return;
      if (event.defaultPrevented) return;

      event.preventDefault();
      if (!form.reportValidity()) return;

      const progress = window.parent !== window && window.parent.SkyProg
        ? window.parent.SkyProg
        : window.SkyProg;

      save.disabled = true;
      progress?.Show();

      let response;
      let html = "";
      let error = "";

      try {
        response = await fetch(form.action || window.location.href, {
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
        const complete = () => {
          save.disabled = false;
          if (error) {
            window.SkyLabMessageBox?.show({ title: "SkyLab - attenzione", message: error, variant: "error" });
            return;
          }

          if (response?.redirected) {
            window.location.href = response.url;
            return;
          }

          document.open();
          document.write(html);
          document.close();
        };

        if (progress) progress.Close(complete);
        else complete();
      }
    });
  });
})();
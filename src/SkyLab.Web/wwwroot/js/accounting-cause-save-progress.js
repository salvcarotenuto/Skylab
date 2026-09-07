(() => {
  const form = document.getElementById("cause-edit-form");
  const button = document.querySelector("[data-cause-edit-save]");
  if (!form || !button) return;

  let saving = false;

  const showError = (message) => {
    if (window.SkyLabMessageBox?.show) {
      window.SkyLabMessageBox.show({
        title: "SkyLab - attenzione",
        message,
        variant: "error"
      });
    } else {
      window.alert(message);
    }
  };

  form.addEventListener("submit", async (event) => {
    if (event.defaultPrevented) return;
    if (!form.reportValidity()) return;

    event.preventDefault();
    if (saving) return;

    const progress = window.parent !== window && window.parent.SkyProg
      ? window.parent.SkyProg
      : window.SkyProg;

    saving = true;
    button.disabled = true;
    progress?.Show();

    let response;
    let html = "";
    let error = "";
    let destination = "";
    const listUrl = "/Tabelle/CausaliContabili/Index";

    try {
      response = await fetch(form.action || location.href, {
        method: "POST",
        body: new FormData(form),
        credentials: "same-origin",
        headers: { "X-Requested-With": "XMLHttpRequest" },
        redirect: "follow"
      });
      const contentType = response.headers.get("content-type") || "";
      if (contentType.includes("application/json")) {
        const data = await response.json();
        destination = data?.url || "";
        if (!response.ok) {
          error = data?.message || "Salvataggio non riuscito.";
        }
      } else {
        html = await response.text();
      }
      if (!response.ok && !error) {
        error = "Salvataggio non riuscito.";
      }
    } catch {
      error = "Impossibile completare il salvataggio.";
    } finally {
      const complete = () => {
        saving = false;
        if (button.isConnected) button.disabled = false;

        if (error) {
          showError(error);
          return;
        }

        if (destination) {
          location.href = destination;
          return;
        }

        if (response?.ok) {
          location.href = listUrl;
          return;
        }

        if (response?.redirected || (response?.url && response.url !== location.href)) {
          location.href = response.url;
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
})();

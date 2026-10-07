document.addEventListener("DOMContentLoaded", () => {
  const form = document.querySelector("#user-form");
  const save = document.querySelector("[data-user-save]");
  const cancel = document.querySelector("[data-user-cancel]");
  const view = document.querySelector("[data-password-view]");
  const password = document.querySelector("[data-password-input]");
  if (!form || !save) return;

  cancel?.addEventListener("click", () => {
    location.href = cancel.dataset.cancelUrl || "/Utenti";
  });

  document.addEventListener("keydown", event => {
    if (event.key !== "Escape" || event.defaultPrevented || document.querySelector("dialog[open]")) return;
    event.preventDefault();
    cancel?.click();
  });

  view?.addEventListener("click", () => {
    const visible = password.type === "text";
    password.type = visible ? "password" : "text";
    view.textContent = visible ? "Vedi" : "Nascondi";
  });

  save.addEventListener("click", () => form.requestSubmit());
  form.addEventListener("submit", async event => {
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
        save.disabled = false;
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
});

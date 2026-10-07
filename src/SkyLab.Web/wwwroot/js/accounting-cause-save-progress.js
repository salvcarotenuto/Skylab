(() => {
  const form = document.getElementById("cause-edit-form");
  const button = document.querySelector("[data-cause-edit-save]");
  const page = document.querySelector("[data-cause-edit-page]");
  if (!form || !button || !page) return;

  let saving = false;
  let authorizing = false;
  const showError = (message) => window.SkyLabMessageBox?.show({ title: "SkyLab - attenzione", message, variant: "error" });

  const save = async (password = "") => {
    if (saving) return;
    const progress = window.parent !== window && window.parent.SkyProg ? window.parent.SkyProg : window.SkyProg;
    saving = true;
    button.disabled = true;
    progress?.Show();

    let response;
    let html = "";
    let error = "";
    let destination = "";
    try {
      const data = new FormData(form);
      data.set("AssistancePassword", password);
      response = await fetch(form.action || location.href, {
        method: "POST", body: data, credentials: "same-origin",
        headers: { "X-Requested-With": "XMLHttpRequest" }, redirect: "follow"
      });
      const contentType = response.headers.get("content-type") || "";
      if (contentType.includes("application/json")) {
        const result = await response.json();
        destination = result?.url || "";
        if (!response.ok) error = result?.message || "Salvataggio non riuscito.";
      } else html = await response.text();
      if (!response.ok && !error) error = "Salvataggio non riuscito.";
    } catch {
      error = "Impossibile completare il salvataggio.";
    } finally {
      const complete = () => {
        saving = false;
        if (button.isConnected) button.disabled = false;
        if (error) { showError(error); return; }
        if (destination) { location.href = destination; return; }
        if (response?.ok) { location.href = "/Tabelle/CausaliContabili/Index"; return; }
        if (response?.redirected || (response?.url && response.url !== location.href)) { location.href = response.url; return; }
        document.open(); document.write(html); document.close();
      };
      if (progress) progress.Close(complete); else complete();
    }
  };

  const validateAssistancePassword = async (password) => {
    if (authorizing || saving) return;
    authorizing = true;
    button.disabled = true;
    try {
      const data = new FormData(form);
      data.set("AssistancePassword", password);
      const url = new URL(form.action || location.href, location.href);
      url.searchParams.set("handler", "ValidateAssistance");
      const response = await fetch(url, {
        method: "POST", body: data, credentials: "same-origin",
        headers: { "X-Requested-With": "XMLHttpRequest" }
      });
      const result = await response.json().catch(() => null);
      if (!response.ok || !result?.ok) {
        showError(result?.message || "Password non corretta.");
        return;
      }
      await save(password);
    } catch {
      showError("Impossibile verificare la password.");
    } finally {
      authorizing = false;
      if (button.isConnected && !saving) button.disabled = false;
    }
  };

  form.addEventListener("submit", (event) => {
    if (event.defaultPrevented) return;
    event.preventDefault();
    if (saving || authorizing || !form.reportValidity()) return;
    if (page.dataset.causeLocked !== "true") { save(); return; }
    window.SkyLabMessageBox?.show({
      mode: "confirm", variant: "confirm", title: "Causale bloccata",
      message: "Questa causale è bloccata",
      detail: "Inserisci la password di sicurezza per procedere",
      input: { type: "password", label: "Password" }, okText: "Conferma", cancelText: "Annulla",
      onConfirm: (password) => {
        if (!password) { showError("Password non corretta."); return; }
        validateAssistancePassword(password);
      }
    });
  });
})();

(function () {
  function ensureMessageBox() {
    let overlay = document.querySelector("[data-skylab-messagebox]");
    if (overlay) return overlay;
    overlay = document.createElement("dialog");
    overlay.className = "messagebox-overlay";
    overlay.dataset.skylabMessagebox = "true";
    overlay.setAttribute("role", "alertdialog");
    overlay.setAttribute("aria-modal", "true");
    overlay.setAttribute("aria-labelledby", "messagebox-title");
    overlay.setAttribute("aria-describedby", "messagebox-body");
    overlay.innerHTML = `<div class="messagebox"><div class="messagebox-title" id="messagebox-title"></div><div class="messagebox-body" id="messagebox-body"><div class="messagebox-message"></div><div class="messagebox-detail"></div><label class="messagebox-input-label" hidden></label><input class="messagebox-input" hidden /></div><div class="messagebox-actions"><button class="btn btn-primary messagebox-ok" type="button">OK</button><button class="btn btn-outline-secondary messagebox-cancel" type="button">Annulla</button></div></div>`;
    document.body.appendChild(overlay);
    return overlay;
  }

  function show(options = {}) {
    const overlay = ensureMessageBox();
    const title = overlay.querySelector(".messagebox-title");
    const message = overlay.querySelector(".messagebox-message");
    const detail = overlay.querySelector(".messagebox-detail");
    const okButton = overlay.querySelector(".messagebox-ok");
    const cancelButton = overlay.querySelector(".messagebox-cancel");
    const inputLabel = overlay.querySelector(".messagebox-input-label");
    const input = overlay.querySelector(".messagebox-input");
    const previousFocus = document.activeElement;
    const mode = options.mode || "alert";
    const variant = options.variant || (mode === "confirm" ? "confirm" : "info");
    title.textContent = options.title || "SkyLab - messaggio";
    message.textContent = options.message || "";
    detail.textContent = options.detail || "";
    detail.hidden = !options.detail;
    inputLabel.hidden = !options.input;
    input.hidden = !options.input;
    input.type = options.input?.type || "text";
    input.value = "";
    inputLabel.textContent = options.input?.label || "";
    input.setAttribute("aria-label", options.input?.label || "Inserisci valore");
    okButton.textContent = options.okText || "OK";
    cancelButton.textContent = options.cancelText || "Annulla";
    cancelButton.hidden = mode !== "confirm";
    overlay.classList.remove("messagebox-info", "messagebox-success", "messagebox-error", "messagebox-confirm");
    overlay.classList.add(`messagebox-${variant}`);
    let closed = false;
    const close = (confirmed) => {
      if (closed) return;
      closed = true;
      overlay.removeEventListener("cancel", onCancelEvent);
      okButton.removeEventListener("click", onOk);
      cancelButton.removeEventListener("click", onCancel);
      document.removeEventListener("keydown", onKeyDown, true);
      if (overlay.open) overlay.close();
      if (confirmed && typeof options.onConfirm === "function") options.onConfirm(options.input ? input.value : undefined);
      else if (!confirmed && typeof options.onCancel === "function") options.onCancel();
      else previousFocus?.focus?.();
      window.dispatchEvent(new CustomEvent("skylab:messagebox-closed", { detail: { confirmed } }));
    };
    const onOk = () => close(true);
    const onCancel = () => close(false);
    const onCancelEvent = (event) => { event.preventDefault(); close(mode !== "confirm"); };
    const onKeyDown = (event) => {
      if (event.key === "Enter") { event.preventDefault(); close(true); }
    };
    okButton.addEventListener("click", onOk);
    cancelButton.addEventListener("click", onCancel);
    overlay.addEventListener("cancel", onCancelEvent);
    document.addEventListener("keydown", onKeyDown, true);
    overlay.showModal();
    if (options.input) input.focus();
    else (mode === "confirm" ? cancelButton : okButton).focus();
  }
  window.SkyLabMessageBox = { show };
})();

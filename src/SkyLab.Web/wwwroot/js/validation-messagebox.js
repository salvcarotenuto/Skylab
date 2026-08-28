(function () {
  const textOf = (element) => (element.textContent || "").replace(/\s+/g, " ").trim();
  function collectMessages() {
    const messages = [...Array.from(document.querySelectorAll(".validation-summary-errors li")).map(textOf), ...Array.from(document.querySelectorAll(".field-validation-error")).map(textOf)];
    return [...new Set(messages.filter(Boolean))];
  }
  function firstInvalidControl() {
    const error = document.querySelector(".field-validation-error");
    return error?.closest(".field")?.querySelector("input, select, textarea") || document.querySelector(".input-validation-error");
  }
  function showValidationBox(fallbackMessage = "") {
    const messages = collectMessages();
    const message = messages[0] || fallbackMessage;
    if (!message || !window.SkyLabMessageBox) return;
    window.SkyLabMessageBox.show({title:"SkyLab - attenzione",message,variant:"error",okText:"OK",onConfirm:()=>firstInvalidControl()?.focus?.()});
  }
  document.addEventListener("DOMContentLoaded", () => {
    showValidationBox();
    if (window.jQuery) window.jQuery(document).on("invalid-form.validate", () => window.setTimeout(showValidationBox, 0));
    document.addEventListener("invalid", (event) => {
      event.preventDefault();
      const control = event.target;
      const message = control.getAttribute?.("data-val-required") || control.validationMessage || "Controllare i dati inseriti.";
      window.setTimeout(() => showValidationBox(message), 0);
    }, true);
  });
  window.SkyLabValidationMessageBox = { show: showValidationBox };
})();

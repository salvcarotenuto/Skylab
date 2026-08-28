document.addEventListener("DOMContentLoaded", () => {
  const form = document.querySelector("#customer-form");
  const name = document.querySelector("#Cliente_Name");
  if (!form || !name) return;

  const showRequiredName = (event) => {
    if (name.value.trim() !== "") return false;
    event?.preventDefault();
    event?.stopImmediatePropagation();
    window.SkyLabMessageBox?.show({
      title: "SkyLab - attenzione",
      message: name.getAttribute("data-val-required") || "Inserire il nome cliente.",
      variant: "error",
      okText: "OK",
      onConfirm: () => name.focus()
    });
    return true;
  };

  document.addEventListener("click", (event) => {
    const submitter = event.target.closest("button[type='submit'], input[type='submit']");
    if (!submitter) return;
    const targetForm = submitter.form || (submitter.getAttribute("form") === form.id ? form : null);
    if (targetForm === form) showRequiredName(event);
  }, true);

  form.addEventListener("submit", showRequiredName, true);
});

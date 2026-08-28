document.addEventListener("DOMContentLoaded", () => {
  const deleteButton = document.querySelector("[data-machine-delete]");
  const deleteSubmit = document.querySelector("[data-machine-delete-submit]");
  if (!deleteButton || !deleteSubmit) return;

  deleteButton.addEventListener("click", () => {
    const article = document.querySelector("[data-machine-article-code]")?.value?.trim() || "selezionata";
    window.SkyLabMessageBox?.show({
      mode: "confirm",
      variant: "confirm",
      title: "Elimina macchina",
      message: `Eliminare la macchina ${article}?`,
      detail: "L'operazione non può essere annullata.",
      okText: "Elimina",
      cancelText: "Annulla",
      onConfirm: () => deleteSubmit.click()
    });
  });
});

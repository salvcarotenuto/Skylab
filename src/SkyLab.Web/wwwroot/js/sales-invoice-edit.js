document.addEventListener("DOMContentLoaded", () => {
  const workDescription = document.querySelector("[data-sales-invoice-work-description]");
  if (!workDescription) return;

  workDescription.addEventListener("blur", () => {
    workDescription.scrollTop = 0;
    workDescription.scrollLeft = 0;
  });
});

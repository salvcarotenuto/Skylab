document.addEventListener("DOMContentLoaded", () => {
  const form = document.querySelector("[data-installed-filter-form]");
  if (!form) return;

  let timer = 0;
  const submit = () => form.requestSubmit ? form.requestSubmit() : form.submit();
  const submitSoon = () => {
    window.clearTimeout(timer);
    timer = window.setTimeout(submit, 350);
  };

  form.querySelectorAll("select, [data-installed-date-hidden]").forEach(control => {
    control.addEventListener("change", submit);
  });

  form.querySelectorAll("input[type='text']:not([data-filter-date-display]), input[type='search']").forEach(control => {
    control.addEventListener("input", submitSoon);
    control.addEventListener("change", submit);
  });
});

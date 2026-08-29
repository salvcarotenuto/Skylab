document.addEventListener("DOMContentLoaded", () => {
  document.querySelectorAll("form[data-enter-navigation]").forEach((form) => {
    const controls = () => Array.from(form.querySelectorAll("input, select, textarea"))
      .filter((control) =>
        !control.disabled &&
        !control.readOnly &&
        control.type !== "hidden" &&
        control.tabIndex >= 0 &&
        control.offsetParent !== null);

    form.addEventListener("keydown", (event) => {
      if (event.key !== "Enter" || event.altKey || event.ctrlKey || event.metaKey) return;
      if (event.target instanceof HTMLTextAreaElement) return;
      const fields = controls();
      const index = fields.indexOf(event.target);
      if (index < 0 || fields.length < 2) return;
      event.preventDefault();
      const direction = event.shiftKey ? -1 : 1;
      const next = fields[(index + direction + fields.length) % fields.length];
      next.focus();
      if (next instanceof HTMLInputElement && next.type === "text") next.select();
    });
  });
});

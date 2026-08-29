document.addEventListener("DOMContentLoaded", () => {
  document.querySelectorAll("[data-unlock-date]").forEach((box) => {
    const display = box.querySelector("[data-filter-date-display]");
    const hidden = box.querySelector("[data-filter-date-hidden]");
    const calendar = box.querySelector("[data-date-picker-button]");
    const toggle = box.querySelector("[data-date-unlock-button]");
    const originalDisplay = display.value;
    const originalHidden = hidden.value;
    toggle.addEventListener("click", () => {
      const unlocking = display.readOnly;
      if (unlocking) {
        display.readOnly = false; display.tabIndex = 0; calendar.disabled = false;
        toggle.textContent = "Annulla"; display.focus(); display.select();
      } else {
        display.value = originalDisplay; hidden.value = originalHidden;
        display.readOnly = true; display.tabIndex = -1; calendar.disabled = true;
        toggle.textContent = "Modifica";
      }
    });
  });
});

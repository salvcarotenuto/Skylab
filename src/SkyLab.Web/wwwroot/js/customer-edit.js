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

  const city = document.querySelector("#Cliente_City");
  const postalCode = document.querySelector("#Cliente_PostalCode");
  const province = document.querySelector("#Cliente_Province");
  const suggestions = document.querySelector("#customer-city-suggestions");
  if (!city || !postalCode || !province || !suggestions) return;
  let timer = 0;
  let selectedIndex = -1;
  let results = [];

  const closeSuggestions = () => { suggestions.hidden = true; suggestions.replaceChildren(); selectedIndex = -1; };
  const choose = (item) => {
    city.value = item.name || "";
    postalCode.value = item.postalCode || "";
    province.value = (item.province || "").toUpperCase();
    closeSuggestions();
    postalCode.focus();
  };
  const select = (index) => {
    const buttons = [...suggestions.querySelectorAll("button")];
    if (!buttons.length) return;
    selectedIndex = Math.max(0, Math.min(index, buttons.length - 1));
    buttons.forEach((button, i) => button.classList.toggle("selected", i === selectedIndex));
    buttons[selectedIndex].scrollIntoView({ block: "nearest" });
  };
  const render = () => {
    suggestions.replaceChildren(...results.map((item, index) => {
      const button = document.createElement("button");
      button.type = "button";
      button.setAttribute("role", "option");
      button.innerHTML = `<strong>${item.name}</strong><span>${item.postalCode}${item.province ? ` · ${item.province}` : ""}</span>`;
      button.addEventListener("mousedown", event => { event.preventDefault(); choose(item); });
      button.addEventListener("mouseenter", () => select(index));
      return button;
    }));
    suggestions.hidden = results.length === 0;
    selectedIndex = -1;
  };
  city.addEventListener("input", () => {
    clearTimeout(timer);
    const query = city.value.trim();
    if (query.length < 2) { closeSuggestions(); return; }
    timer = setTimeout(async () => {
      try {
        const response = await fetch(`?handler=Cities&q=${encodeURIComponent(query)}`);
        results = response.ok ? await response.json() : [];
        render();
      } catch { closeSuggestions(); }
    }, 180);
  });
  city.addEventListener("keydown", event => {
    if (suggestions.hidden) return;
    if (event.key === "ArrowDown") { event.preventDefault(); select(selectedIndex + 1); }
    else if (event.key === "ArrowUp") { event.preventDefault(); select(selectedIndex < 0 ? results.length - 1 : selectedIndex - 1); }
    else if (event.key === "Enter" && selectedIndex >= 0) { event.preventDefault(); choose(results[selectedIndex]); }
    else if (event.key === "Escape") { event.preventDefault(); closeSuggestions(); }
  });
  city.addEventListener("blur", () => setTimeout(closeSuggestions, 120));
});

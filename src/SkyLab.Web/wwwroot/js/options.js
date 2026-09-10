document.addEventListener("DOMContentLoaded", () => {
  const form = document.getElementById("options-form");
  const saveButton = document.querySelector('button[form="options-form"][type="submit"]');
  let saving = false;

  const closeProgress = (afterClose) => {
    const progress = window.SkyProg;
    if (progress?.close) {
      progress.close(afterClose);
    } else if (progress?.Close) {
      progress.Close(afterClose);
    } else {
      afterClose?.();
    }
  };

  form?.addEventListener("submit", async (event) => {
    event.preventDefault();
    if (saving) return;

    saving = true;
    if (saveButton) saveButton.disabled = true;
    window.SkyProg?.Show?.();

    let destination = "";
    let returnedPage = "";
    let errorMessage = "";
    try {
      const response = await fetch(form.action || window.location.href, {
        method: "POST",
        body: new FormData(form),
        credentials: "same-origin",
        redirect: "follow",
        headers: { "X-Requested-With": "XMLHttpRequest" }
      });

      if (!response.ok) {
        errorMessage = "Salvataggio Opzioni non riuscito.";
      } else if (response.redirected) {
        destination = response.url;
      } else {
        returnedPage = await response.text();
      }
    } catch {
      errorMessage = "Salvataggio Opzioni non riuscito.";
    } finally {
      closeProgress(() => {
        saving = false;
        if (saveButton?.isConnected) saveButton.disabled = false;

        if (errorMessage) {
          window.SkyLabMessageBox?.show?.({
            title: "Opzioni",
            message: errorMessage,
            variant: "error"
          }) ?? window.alert(errorMessage);
          return;
        }

        if (destination) {
          window.location.href = destination;
          return;
        }

        if (returnedPage) {
          document.open();
          document.write(returnedPage);
          document.close();
        }
      });
    }
  });

  const message = (text, target) => {
    const show = window.SkyLabMessageBox?.show;
    if (!show) {
      window.alert(text);
      target?.focus?.();
      return;
    }

    show({
      title: "Opzioni",
      message: text,
      variant: "error",
      okText: "OK",
      onConfirm: () => target?.focus?.()
    });
  };

  const normalizeFiscalValue = (value) => String(value || "").toUpperCase().replace(/[^A-Z0-9]/g, "");
  const taxCode = document.querySelector("[data-options-tax-code]");
  const vatNumber = document.querySelector("[data-options-vat-number]");
  const stateCode = document.querySelector("[data-options-state-code]");

  const isValidVatNumber = (value) => {
    const vat = normalizeFiscalValue(value);
    if (!/^\d{11}$/.test(vat)) return false;

    let sum = 0;
    for (let index = 0; index < vat.length; index += 1) {
      const digit = Number(vat[index]);
      if (index % 2 === 0) {
        sum += digit;
      } else {
        const doubled = digit * 2;
        sum += doubled > 9 ? doubled - 9 : doubled;
      }
    }

    return sum % 10 === 0;
  };

  const oddFiscalCodeValues = {
    0: 1, 1: 0, 2: 5, 3: 7, 4: 9, 5: 13, 6: 15, 7: 17, 8: 19, 9: 21,
    A: 1, B: 0, C: 5, D: 7, E: 9, F: 13, G: 15, H: 17, I: 19, J: 21,
    K: 2, L: 4, M: 18, N: 20, O: 11, P: 3, Q: 6, R: 8, S: 12, T: 14,
    U: 16, V: 10, W: 22, X: 25, Y: 24, Z: 23
  };

  const evenFiscalCodeValue = (character) => {
    if (/^\d$/.test(character)) return Number(character);
    const code = character.charCodeAt(0);
    return code >= 65 && code <= 90 ? code - 65 : -1;
  };

  const isValidPersonalFiscalCode = (value) => {
    const fiscalCode = normalizeFiscalValue(value);
    if (!/^[A-Z0-9]{16}$/.test(fiscalCode)) return false;

    let sum = 0;
    for (let index = 0; index < 15; index += 1) {
      const character = fiscalCode[index];
      const digit = index % 2 === 0 ? oddFiscalCodeValues[character] : evenFiscalCodeValue(character);
      if (digit === undefined || digit < 0) return false;
      sum += digit;
    }

    return fiscalCode[15] === String.fromCharCode(65 + (sum % 26));
  };

  const isValidFiscalCode = (value) => {
    const current = normalizeFiscalValue(value);
    return current.length === 11 ? isValidVatNumber(current) : isValidPersonalFiscalCode(current);
  };

  taxCode?.addEventListener("input", () => {
    taxCode.value = normalizeFiscalValue(taxCode.value).slice(0, 16);
  });

  taxCode?.addEventListener("blur", () => {
    const value = normalizeFiscalValue(taxCode.value);
    taxCode.value = value;
    if (!value) return;
    if (![11, 16].includes(value.length) || !isValidFiscalCode(value)) {
      message("Codice fiscale non valido.", taxCode);
    }
  });

  vatNumber?.addEventListener("input", () => {
    vatNumber.value = String(vatNumber.value || "").replace(/\D/g, "").slice(0, 11);
  });

  vatNumber?.addEventListener("blur", () => {
    const value = String(vatNumber.value || "").replace(/\D/g, "").slice(0, 11);
    vatNumber.value = value;
    if (!value) return;
    if (!isValidVatNumber(value)) {
      message("Partita IVA non valida.", vatNumber);
    }
  });

  stateCode?.addEventListener("input", () => {
    stateCode.value = String(stateCode.value || "").toUpperCase().replace(/[^A-Z]/g, "").slice(0, 2);
  });

  stateCode?.addEventListener("blur", () => {
    stateCode.value = String(stateCode.value || "").toUpperCase().replace(/[^A-Z]/g, "").slice(0, 2);
    if (stateCode.value && stateCode.value.length !== 2) {
      message("Sigla stato non valida.", stateCode);
    }
  });

  const wireCityGuide = (scope) => {
    const city = document.querySelector(`[data-options-city="${scope}"]`);
    const postalCode = document.querySelector(`[data-options-postal-code="${scope}"]`);
    const province = document.querySelector(`[data-options-province="${scope}"]`);
    const list = document.getElementById(`options-${scope}-city-suggestions`);
    if (!city || !postalCode || !province || !list) return;

    let timer = 0;
    let selected = -1;
    let requestId = 0;

    const close = () => {
      list.hidden = true;
      list.replaceChildren();
      selected = -1;
    };

    const choose = (item) => {
      city.value = item.name || "";
      postalCode.value = item.postalCode || "";
      province.value = String(item.province || "").toUpperCase();
      close();
      postalCode.focus();
    };

    const highlight = (index) => {
      const buttons = Array.from(list.querySelectorAll("button"));
      if (!buttons.length) return;
      selected = (index + buttons.length) % buttons.length;
      buttons.forEach((button, buttonIndex) => button.classList.toggle("selected", buttonIndex === selected));
      buttons[selected].scrollIntoView({ block: "nearest" });
    };

    const render = (items) => {
      list.replaceChildren();
      selected = -1;

      for (const item of items) {
        const button = document.createElement("button");
        button.type = "button";
        button.setAttribute("role", "option");
        const name = document.createElement("strong");
        name.textContent = item.name || "";
        const postalCode = document.createElement("span");
        postalCode.textContent = item.postalCode || "";
        const province = document.createElement("span");
        province.textContent = item.province || "";
        button.append(name, postalCode, province);
        button.addEventListener("mousedown", (event) => {
          event.preventDefault();
          choose(item);
        });
        list.append(button);
      }
      list.hidden = items.length === 0;
    };

    const search = async () => {
      const term = city.value.trim();
      if (term.length < 3) {
        close();
        return;
      }

      const current = ++requestId;
      try {
        const response = await fetch(`${location.pathname}?handler=Cities&q=${encodeURIComponent(term)}`, {
          headers: { Accept: "application/json" }
        });
        if (!response.ok || current !== requestId) return;
        render(await response.json());
      } catch {
        if (current === requestId) close();
      }
    };

    city.addEventListener("input", () => {
      clearTimeout(timer);
      timer = window.setTimeout(search, 180);
    });

    city.addEventListener("keydown", (event) => {
      const buttons = list.querySelectorAll("button");
      if (event.key === "ArrowDown" && buttons.length) {
        event.preventDefault();
        highlight(selected < 0 ? 0 : selected + 1);
      } else if (event.key === "ArrowUp" && buttons.length) {
        event.preventDefault();
        highlight(selected < 0 ? buttons.length - 1 : selected - 1);
      } else if ((event.key === "Enter" || event.key === "Tab") && selected >= 0) {
        event.preventDefault();
        buttons[selected].dispatchEvent(new MouseEvent("mousedown"));
      } else if (event.key === "Escape") {
        close();
      }
    });

    city.addEventListener("blur", () => window.setTimeout(close, 120));
  };

  wireCityGuide("legal");
  wireCityGuide("operational");
});

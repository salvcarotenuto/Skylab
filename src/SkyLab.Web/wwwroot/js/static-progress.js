(() => {
  "use strict";

  class SkyLabStaticProgress {
    static #panel = null;
    static #bar = null;
    static #run = 0;
    static #closing = false;
    static #timers = [];
    static #afterClose = null;

    static Show() {
      this.#run += 1;
      this.#timers.forEach(timer => window.clearTimeout(timer));
      this.#timers = [];
      this.#panel?.remove();

      const panel = document.createElement("div");
      panel.className = "skylab-static-progress";
      panel.setAttribute("role", "progressbar");
      panel.setAttribute("aria-label", "Operazione in corso");
      panel.setAttribute("aria-valuemin", "0");
      panel.setAttribute("aria-valuemax", "100");
      panel.setAttribute("aria-valuenow", "4");

      const baseBar = document.createElement("div");
      baseBar.className = "skylab-static-progress-base";
      baseBar.dataset.role = "BaseBar";

      const bar = document.createElement("div");
      bar.className = "skylab-static-progress-bar";
      bar.dataset.role = "Bar";

      baseBar.appendChild(bar);
      panel.appendChild(baseBar);
      document.body.appendChild(panel);

      this.#panel = panel;
      this.#bar = bar;
      this.#closing = false;
      this.#afterClose = null;
    }

    static Close(afterClose) {
      if (typeof afterClose === "function") this.#afterClose = afterClose;
      if (!this.#panel || !this.#bar) {
        const callback = this.#afterClose;
        this.#afterClose = null;
        callback?.();
        return;
      }
      if (this.#closing) return;

      const run = this.#run;
      const panel = this.#panel;
      const bar = this.#bar;
      this.#closing = true;
      bar.style.transition = "width 30ms linear";

      [12, 24, 37, 50, 63, 76, 89, 100].forEach((value, index) => {
        const delay = index * 30;
        const width = value === 100 ? "calc(100% - 2px)" : `${value}%`;
        this.#timers.push(window.setTimeout(() => {
          if (run !== this.#run) return;
          bar.style.width = width;
          panel.setAttribute("aria-valuenow", String(value));
        }, delay));
      });

      this.#timers.push(window.setTimeout(() => {
        if (run !== this.#run) return;
        panel.remove();
        this.#panel = null;
        this.#bar = null;
        this.#closing = false;
        this.#timers = [];
        const callback = this.#afterClose;
        this.#afterClose = null;
        callback?.();
      }, 460));
    }
  }

  window.SkyProg = SkyLabStaticProgress;
})();

(() => {
    const page = document.querySelector('[data-work-list-page]');
    if (!page) return;
    const form = page.querySelector('[data-work-filter-form]');
    const search = page.querySelector('[data-work-search]');
    const rows = [...page.querySelectorAll('[data-work-row]')];
    const count = page.querySelector('[data-work-count]');
    const empty = page.querySelector('[data-work-empty]');

    const normalize = value => (value || '').toLocaleLowerCase('it-IT').normalize('NFD').replace(/[\u0300-\u036f]/g, '');
    const filter = () => {
        const term = normalize(search.value.trim());
        let visible = 0;
        rows.forEach(row => {
            const show = !term || normalize(row.dataset.filter).includes(term);
            row.hidden = !show;
            if (show) visible++;
        });
        count.textContent = visible;
        empty.hidden = visible !== 0;
    };
    search.addEventListener('input', filter);
    document.addEventListener('keydown', event => {
        const target = event.target;
        const isEditable = target instanceof HTMLInputElement ||
            target instanceof HTMLTextAreaElement ||
            target instanceof HTMLSelectElement ||
            target instanceof HTMLButtonElement ||
            target?.isContentEditable;

        if (event.defaultPrevented || isEditable || event.altKey || event.ctrlKey || event.metaKey) return;

        if (event.key.length === 1) {
            event.preventDefault();
            search.value += event.key;
            search.focus({ preventScroll: true });
            filter();
        } else if (event.key === 'Backspace' && search.value !== '') {
            event.preventDefault();
            search.value = search.value.slice(0, -1);
            search.focus({ preventScroll: true });
            filter();
        }
    });
    form.querySelector('[data-work-order]').addEventListener('change', () => {
        form.querySelectorAll('[data-work-date]').forEach(control => control.value = '');
        form.submit();
    });
    form.querySelectorAll('[data-work-date]').forEach(control => control.addEventListener('change', () => form.submit()));
    form.querySelectorAll('select:not([data-work-order])').forEach(control => control.addEventListener('change', () => form.submit()));
})();

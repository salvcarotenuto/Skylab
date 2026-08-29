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
    form.querySelector('[data-work-order]').addEventListener('change', () => {
        form.querySelector('#Da').value = '';
        form.submit();
    });
    form.querySelectorAll('select:not([data-work-order])').forEach(control => control.addEventListener('change', () => form.submit()));
})();

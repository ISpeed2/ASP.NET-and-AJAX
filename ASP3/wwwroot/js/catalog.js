(function () {
    'use strict';

    const root = document.getElementById('catalog');
    if (!root) return;

    const endpoint = root.dataset.endpoint;
    const errorBox = document.getElementById('catalog-error');
    const template = document.getElementById('product-card-template');

    function showError(message) {
        errorBox.textContent = message;
        errorBox.classList.remove('d-none');
    }

    function clearError() {
        errorBox.textContent = '';
        errorBox.classList.add('d-none');
    }

    function render(products) {
        if (!products.length) {
            root.innerHTML = '<p class="text-muted">Ничего не найдено.</p>';
            return;
        }

        const fragment = document.createDocumentFragment();
        products.forEach(product => {
            const node = template.content.cloneNode(true);
            node.querySelector('[data-field="name"]').textContent = product.name;
            node.querySelector('[data-field="category"]').textContent = product.category;
            node.querySelector('[data-field="price"]').textContent =
                new Intl.NumberFormat('ru-RU').format(product.price);
            node.querySelector('[data-field="details"]').dataset.id = product.id;
            fragment.appendChild(node);
        });

        root.replaceChildren(fragment);
    }

    async function load(category) {
        clearError();
        root.innerHTML = `<p class="text-muted">${root.dataset.loadingText}</p>`;
        const url = category
            ? `${endpoint}?category=${encodeURIComponent(category)}`
            : endpoint;

        try {
            const response = await fetch(url, { headers: { Accept: 'application/json' } });
            if (!response.ok) throw new Error(`HTTP ${response.status}`);
            render(await response.json());
        } catch (error) {
            console.error(error);
            showError('Не удалось загрузить каталог: ' + error.message);
        }
    }

    document.querySelectorAll('.filter-btn').forEach(button => {
        button.addEventListener('click', () => load(button.dataset.category || null));
    });

    root.addEventListener('click', async event => {
        const button = event.target.closest('.details-btn');
        if (!button) return;

        try {
            const response = await fetch(`${endpoint}/${button.dataset.id}`);
            if (!response.ok) throw new Error(`HTTP ${response.status}`);
            const product = await response.json();
            alert(`${product.name}\nКатегория: ${product.category}\nЦена: ${product.price} ₽`);
        } catch (error) {
            showError('Не удалось загрузить товар: ' + error.message);
        }
    });

    load(null);
})();

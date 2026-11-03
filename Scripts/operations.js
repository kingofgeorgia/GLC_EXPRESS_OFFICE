(function () {
    'use strict';
    var root = document.querySelector('.ops-app');
    if (!root) return;
    var dialogs = Array.prototype.slice.call(root.querySelectorAll('dialog'));
    function enable(dialog, enabled) {
        dialog.querySelectorAll('input, select, button[type="submit"]').forEach(function (field) { field.disabled = !enabled; });
    }
    dialogs.forEach(function (dialog) {
        enable(dialog, false);
        dialog.addEventListener('close', function () { enable(dialog, false); });
    });
    root.addEventListener('click', function (event) {
        var opener = event.target.closest('[data-open]');
        if (opener) {
            var dialog = document.getElementById(opener.dataset.open);
            enable(dialog, true);
            dialog.showModal();
        }
        if (event.target.closest('[data-close]')) event.target.closest('dialog').close();
    });
    var search = document.getElementById('ops-search');
    if (search) {
        function filter() {
            var visible = 0;
            root.querySelectorAll('[data-search-row]').forEach(function (row) {
                row.hidden = row.textContent.toLocaleLowerCase().indexOf(search.value.trim().toLocaleLowerCase()) < 0;
                if (!row.hidden) visible++;
            });
            document.getElementById('ops-empty-search').hidden = visible > 0;
        }
        search.addEventListener('input', filter);
        search.addEventListener('keydown', function (event) { if (event.key === 'Enter') event.preventDefault(); });
        filter();
    }
}());

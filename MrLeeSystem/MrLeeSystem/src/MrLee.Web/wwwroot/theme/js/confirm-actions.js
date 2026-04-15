(() => {
    const modalElement = document.getElementById('appConfirmModal');
    const submitButton = document.getElementById('appConfirmModalSubmit');
    const titleElement = document.getElementById('appConfirmModalTitle');
    const messageElement = document.getElementById('appConfirmModalMessage');
    const eyebrowElement = document.getElementById('appConfirmModalEyebrow');

    if (!modalElement || !submitButton || !window.bootstrap) {
        return;
    }

    const modal = new bootstrap.Modal(modalElement);
    let activeForm = null;

    const setConfirmButtonClass = (className) => {
        submitButton.className = 'btn';
        const classes = (className || 'btn-danger').split(' ').filter(Boolean);
        classes.forEach((value) => submitButton.classList.add(value));
    };

    const resetModal = () => {
        titleElement.textContent = '\u00bfEst\u00e1s seguro?';
        messageElement.textContent = 'Esta acci\u00f3n no se puede deshacer.';
        eyebrowElement.textContent = 'Confirmaci\u00f3n';
        submitButton.textContent = 'Confirmar';
        setConfirmButtonClass('btn-danger');
    };

    document.addEventListener('submit', (event) => {
        const form = event.target;
        if (!(form instanceof HTMLFormElement) || !form.matches('.js-confirm-form')) {
            return;
        }

        if (form.dataset.confirmed === 'true') {
            form.dataset.confirmed = '';
            return;
        }

        event.preventDefault();
        activeForm = form;

        titleElement.textContent = form.dataset.confirmTitle || '\u00bfEst\u00e1s seguro?';
        messageElement.textContent = form.dataset.confirmMessage || 'Esta acci\u00f3n no se puede deshacer.';
        eyebrowElement.textContent = form.dataset.confirmEyebrow || 'Confirmaci\u00f3n';
        submitButton.textContent = form.dataset.confirmButton || 'Confirmar';
        setConfirmButtonClass(form.dataset.confirmButtonClass || 'btn-danger');

        modal.show();
    });

    submitButton.addEventListener('click', () => {
        if (!activeForm) {
            return;
        }

        activeForm.dataset.confirmed = 'true';
        activeForm.requestSubmit();
        activeForm = null;
        modal.hide();
        resetModal();
    });

    modalElement.addEventListener('hidden.bs.modal', () => {
        activeForm = null;
        resetModal();
    });

    resetModal();
})();

(() => {
    "use strict";

    const modal = document.getElementById("download-modal");
    const form = document.getElementById("download-form");
    const emailInput = document.getElementById("email-input");
    const packageInput = document.getElementById("package-input");
    const packageLabel = document.getElementById("modal-package-label");
    const errorBox = document.getElementById("form-error");
    const submitButton = document.getElementById("submit-download");
    const copyNode = document.getElementById("download-copy");
    const countNode = document.getElementById("download-count");

    if (!modal || !form || !emailInput || !packageInput || !packageLabel || !errorBox || !submitButton || !copyNode) {
        return;
    }

    const copy = {
        invalidEmail: copyNode.dataset.invalidEmail || "Enter a valid email address.",
        genericError: copyNode.dataset.genericError || "The download could not be authorized.",
        rateError: copyNode.dataset.rateError || "Too many attempts. Please try again later."
    };

    let previousFocus = null;

    const showError = (message) => {
        errorBox.textContent = message;
        errorBox.hidden = false;
    };

    const clearError = () => {
        errorBox.textContent = "";
        errorBox.hidden = true;
    };

    const openModal = (button) => {
        previousFocus = button;
        packageInput.value = button.dataset.package || "";
        packageLabel.textContent = button.dataset.label || "";
        clearError();
        modal.hidden = false;
        document.body.classList.add("modal-open");
        window.setTimeout(() => emailInput.focus(), 0);
    };

    const closeModal = () => {
        modal.hidden = true;
        document.body.classList.remove("modal-open");
        clearError();
        if (previousFocus instanceof HTMLElement) {
            previousFocus.focus();
        }
    };

    document.querySelectorAll("[data-package]").forEach((button) => {
        button.addEventListener("click", () => openModal(button));
    });

    document.querySelectorAll("[data-close-modal]").forEach((button) => {
        button.addEventListener("click", closeModal);
    });

    document.addEventListener("keydown", (event) => {
        if (event.key === "Escape" && !modal.hidden) {
            closeModal();
        }
    });

    form.addEventListener("submit", async (event) => {
        event.preventDefault();
        clearError();

        if (!emailInput.checkValidity()) {
            showError(copy.invalidEmail);
            emailInput.focus();
            return;
        }

        submitButton.disabled = true;
        const data = new FormData(form);
        data.set("_format", "json");

        try {
            const response = await fetch(form.action, {
                method: "POST",
                body: data,
                credentials: "same-origin",
                headers: {
                    "Accept": "application/json",
                    "X-Requested-With": "XMLHttpRequest"
                }
            });

            const payload = await response.json().catch(() => null);
            if (!response.ok || !payload || payload.ok !== true || typeof payload.download_url !== "string") {
                if (response.status === 422 && payload && payload.error === "invalid_email") {
                    showError(copy.invalidEmail);
                } else if (response.status === 429) {
                    showError(copy.rateError);
                } else {
                    showError(copy.genericError);
                }
                return;
            }

            closeModal();
            window.location.assign(payload.download_url);
        } catch (error) {
            showError(copy.genericError);
        } finally {
            submitButton.disabled = false;
        }
    });

    if (countNode) {
        fetch("/api/count.php", {
            method: "GET",
            credentials: "same-origin",
            headers: { "Accept": "application/json" }
        })
            .then((response) => response.ok ? response.json() : null)
            .then((payload) => {
                if (payload && payload.ok === true && Number.isFinite(Number(payload.count))) {
                    countNode.textContent = new Intl.NumberFormat(document.documentElement.lang || "en").format(Number(payload.count));
                }
            })
            .catch(() => {});
    }
})();

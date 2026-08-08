document.addEventListener("DOMContentLoaded", () => {
    const table = document.querySelector("#contactsTable");

    if (!table) {
        return;
    }

    const tokenInput = document.querySelector(
        "#ajaxAntiforgeryToken input[name='__RequestVerificationToken']"
    );

    const messageBox = document.querySelector(
        "#contactActionMessage"
    );

    function showMessage(type, message) {
        if (!messageBox) {
            return;
        }

        messageBox.className = `alert alert-${type}`;
        messageBox.textContent = message;
        messageBox.classList.remove("d-none");
    }

    function clearMessage() {
        if (!messageBox) {
            return;
        }

        messageBox.classList.add("d-none");
        messageBox.textContent = "";
    }

    function getInput(row, name) {
        return row.querySelector(
            `[data-input="${name}"]`
        );
    }

    function getDisplay(row, name) {
        return row.querySelector(
            `[data-display="${name}"]`
        );
    }

    function restoreInputs(row) {
        getInput(row, "name").value =
            row.dataset.name;

        getInput(row, "dateOfBirth").value =
            row.dataset.dateOfBirth;

        getInput(row, "married").value =
            row.dataset.married;

        getInput(row, "phone").value =
            row.dataset.phone;

        getInput(row, "salary").value =
            Number(row.dataset.salary).toFixed(2);
    }

    function enterEditMode(row) {
        clearMessage();

        document
            .querySelectorAll(
                "#contactsTable tbody tr.editing"
            )
            .forEach(otherRow => {
                if (otherRow !== row) {
                    exitEditMode(otherRow);
                }
            });

        restoreInputs(row);

        row.classList.add("editing");

        row
            .querySelectorAll(".view-value")
            .forEach(element => {
                element.classList.add("d-none");
            });

        row
            .querySelectorAll(".edit-control")
            .forEach(element => {
                element.classList.remove("d-none");
            });

        row
            .querySelector(".view-actions")
            ?.classList.add("d-none");

        row
            .querySelector(".edit-actions")
            ?.classList.remove("d-none");
    }

    function exitEditMode(row) {
        restoreInputs(row);

        row.classList.remove("editing");

        row
            .querySelectorAll(".view-value")
            .forEach(element => {
                element.classList.remove("d-none");
            });

        row
            .querySelectorAll(".edit-control")
            .forEach(element => {
                element.classList.add("d-none");
            });

        row
            .querySelector(".view-actions")
            ?.classList.remove("d-none");

        row
            .querySelector(".edit-actions")
            ?.classList.add("d-none");
    }

    function validateContact(contact) {
        const errors = [];

        if (!contact.name) {
            errors.push("Name is required.");
        }

        if (contact.name.length > 200) {
            errors.push(
                "Name cannot exceed 200 characters."
            );
        }

        if (!contact.dateOfBirth) {
            errors.push(
                "Date of birth is required."
            );
        } else {
            const selectedDate =
                new Date(`${contact.dateOfBirth}T00:00:00`);

            const today = new Date();

            today.setHours(0, 0, 0, 0);

            if (selectedDate > today) {
                errors.push(
                    "Date of birth cannot be in the future."
                );
            }
        }

        if (!contact.phone) {
            errors.push("Phone is required.");
        }

        if (contact.phone.length > 50) {
            errors.push(
                "Phone cannot exceed 50 characters."
            );
        }

        if (!Number.isFinite(contact.salary)) {
            errors.push(
                "Salary must be a valid number."
            );
        } else if (contact.salary < 0) {
            errors.push(
                "Salary cannot be negative."
            );
        }

        return errors;
    }

    async function saveContact(row, button) {
        clearMessage();

        const contact = {
            id: Number(row.dataset.contactId),

            name:
                getInput(row, "name")
                    .value
                    .trim(),

            dateOfBirth:
                getInput(row, "dateOfBirth")
                    .value,

            married:
                getInput(row, "married")
                    .value === "true",

            phone:
                getInput(row, "phone")
                    .value
                    .trim(),

            salary: (() => {
                const rawSalary =
                    getInput(row, "salary")
                        .value
                        .trim();

                return rawSalary === ""
                    ? Number.NaN
                    : Number(rawSalary);
            })()
        };

        const validationErrors =
            validateContact(contact);

        if (validationErrors.length > 0) {
            showMessage(
                "danger",
                validationErrors.join(" ")
            );

            return;
        }

        button.disabled = true;
        button.textContent = "Saving...";

        try {
            const response = await fetch(
                "/Contacts/Update",
                {
                    method: "PUT",

                    headers: {
                        "Content-Type":
                            "application/json",

                        "RequestVerificationToken":
                            tokenInput?.value ?? ""
                    },

                    body: JSON.stringify(contact)
                }
            );

            const data =
                await response
                    .json()
                    .catch(() => ({}));

            if (!response.ok) {
                const errors = data.errors
                    ? Object
                        .values(data.errors)
                        .flat()
                    : [];

                const message =
                    errors.length > 0
                        ? errors.join(" ")
                        : data.message
                            ?? "Unable to update contact.";

                showMessage(
                    "danger",
                    message
                );

                return;
            }

            row.dataset.name =
                contact.name;

            row.dataset.dateOfBirth =
                contact.dateOfBirth;

            row.dataset.married =
                contact.married.toString();

            row.dataset.phone =
                contact.phone;

            row.dataset.salary =
                contact.salary.toString();

            getDisplay(row, "name").textContent =
                contact.name;

            getDisplay(
                row,
                "dateOfBirth"
            ).textContent =
                contact.dateOfBirth;

            getDisplay(
                row,
                "married"
            ).textContent =
                contact.married
                    ? "Yes"
                    : "No";

            getDisplay(
                row,
                "phone"
            ).textContent =
                contact.phone;

            getDisplay(
                row,
                "salary"
            ).textContent =
                contact.salary.toFixed(2);

            exitEditMode(row);

            showMessage(
                "success",
                data.message
                    ?? "Contact updated successfully."
            );
        }
        catch {
            showMessage(
                "danger",
                "Unable to connect to the server."
            );
        }
        finally {
            button.disabled = false;
            button.textContent = "Save";
        }
    }

    async function deleteContact(row, button) {
        clearMessage();

        const id =
            Number(row.dataset.contactId);

        const name =
            row.dataset.name;

        const confirmed = window.confirm(
            `Delete "${name}"?`
        );

        if (!confirmed) {
            return;
        }

        button.disabled = true;
        button.textContent = "Deleting...";

        try {
            const response = await fetch(
                "/Contacts/Delete",
                {
                    method: "DELETE",

                    headers: {
                        "Content-Type":
                            "application/json",

                        "RequestVerificationToken":
                            tokenInput?.value ?? ""
                    },

                    body: JSON.stringify({
                        id
                    })
                }
            );

            const data =
                await response
                    .json()
                    .catch(() => ({}));

            if (!response.ok) {
                showMessage(
                    "danger",
                    data.message
                        ?? "Unable to delete contact."
                );

                return;
            }

            showMessage(
                "success",
                data.message
                    ?? "Contact deleted successfully."
            );

            window.setTimeout(() => {
                window.location.reload();
            }, 400);
        }
        catch {
            showMessage(
                "danger",
                "Unable to connect to the server."
            );

            button.disabled = false;
            button.textContent = "Delete";
        }
    }

    table.addEventListener(
        "click",
        async event => {
            const button =
                event.target.closest("button");

            if (!button) {
                return;
            }

            const row =
                button.closest(
                    "tr[data-contact-id]"
                );

            if (!row) {
                return;
            }

            if (
                button.classList.contains(
                    "edit-contact-btn"
                )
            ) {
                enterEditMode(row);

                return;
            }

            if (
                button.classList.contains(
                    "cancel-edit-btn"
                )
            ) {
                exitEditMode(row);

                return;
            }

            if (
                button.classList.contains(
                    "save-contact-btn"
                )
            ) {
                await saveContact(
                    row,
                    button
                );

                return;
            }

            if (
                button.classList.contains(
                    "delete-contact-btn"
                )
            ) {
                await deleteContact(
                    row,
                    button
                );
            }
        }
    );
});
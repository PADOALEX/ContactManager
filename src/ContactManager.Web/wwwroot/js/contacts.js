document.addEventListener("DOMContentLoaded", () => {
    const table = document.querySelector("#contactsTable");

    if (!table) {
        return;
    }

    const tableBody =
        table.querySelector("#contactsTableBody");

    const contactRows = Array.from(
        table.querySelectorAll(
            "tbody tr[data-contact-id]"
        )
    );

    const noMatchesRow =
        document.querySelector("#noClientMatchesRow");

    const filterColumn =
        document.querySelector("#clientFilterColumn");

    const filterText =
        document.querySelector("#clientFilterText");

    const sortColumn =
        document.querySelector("#clientSortColumn");

    const sortDirection =
        document.querySelector("#clientSortDirection");

    const clearClientToolsButton =
        document.querySelector("#clearClientTools");

    const clientMatchCount =
        document.querySelector("#clientMatchCount");

    const tokenInput = document.querySelector(
        "#ajaxAntiforgeryToken input[name='__RequestVerificationToken']"
    );

    const messageBox = document.querySelector(
        "#contactActionMessage"
    );

    const originalOrder = new Map(
        contactRows.map((row, index) => [
            row.dataset.contactId,
            index
        ])
    );

    const textCollator = new Intl.Collator(
        undefined,
        {
            sensitivity: "base",
            numeric: true
        }
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

    function getMarriedText(row) {
        return row.dataset.married === "true"
            ? "yes true 1"
            : "no false 0";
    }

    function getFilterValue(row, column) {
        switch (column) {
            case "name":
                return row.dataset.name ?? "";

            case "dateOfBirth":
                return row.dataset.dateOfBirth ?? "";

            case "married":
                return getMarriedText(row);

            case "phone":
                return row.dataset.phone ?? "";

            case "salary":
                return row.dataset.salary ?? "";

            case "all":
            default:
                return [
                    row.dataset.name ?? "",
                    row.dataset.dateOfBirth ?? "",
                    getMarriedText(row),
                    row.dataset.phone ?? "",
                    row.dataset.salary ?? ""
                ].join(" ");
        }
    }

    function getSortValue(row, column) {
        switch (column) {
            case "salary":
                return Number(row.dataset.salary);

            case "married":
                return row.dataset.married === "true"
                    ? 1
                    : 0;

            case "dateOfBirth":
                return row.dataset.dateOfBirth ?? "";

            case "phone":
                return row.dataset.phone ?? "";

            case "name":
            default:
                return row.dataset.name ?? "";
        }
    }

    function compareRows(firstRow, secondRow, column) {
        const firstValue =
            getSortValue(firstRow, column);

        const secondValue =
            getSortValue(secondRow, column);

        if (
            column === "salary" ||
            column === "married"
        ) {
            return firstValue - secondValue;
        }

        return textCollator.compare(
            String(firstValue),
            String(secondValue)
        );
    }

    function applyClientTools() {
        if (!tableBody) {
            return;
        }

        const selectedFilterColumn =
            filterColumn?.value ?? "all";

        const searchText =
            (filterText?.value ?? "")
                .trim()
                .toLocaleLowerCase();

        const selectedSortColumn =
            sortColumn?.value ?? "";

        const selectedSortDirection =
            sortDirection?.value ?? "asc";

        const sortedRows = [...contactRows];

        if (selectedSortColumn) {
            sortedRows.sort((firstRow, secondRow) => {
                const comparison =
                    compareRows(
                        firstRow,
                        secondRow,
                        selectedSortColumn
                    );

                return selectedSortDirection === "desc"
                    ? -comparison
                    : comparison;
            });
        } else {
            sortedRows.sort(
                (firstRow, secondRow) =>
                    originalOrder.get(
                        firstRow.dataset.contactId
                    ) -
                    originalOrder.get(
                        secondRow.dataset.contactId
                    )
            );
        }

        sortedRows.forEach(row => {
            tableBody.appendChild(row);
        });

        if (noMatchesRow) {
            tableBody.appendChild(noMatchesRow);
        }

        let matchCount = 0;

        contactRows.forEach(row => {
            const filterValue =
                getFilterValue(
                    row,
                    selectedFilterColumn
                )
                .toLocaleLowerCase();

            const isMatch =
                searchText.length === 0 ||
                filterValue.includes(searchText);

            row.classList.toggle(
                "d-none",
                !isMatch
            );

            if (isMatch) {
                matchCount++;
            }
        });

        if (noMatchesRow) {
            noMatchesRow.classList.toggle(
                "d-none",
                matchCount !== 0
            );
        }

        if (clientMatchCount) {
            clientMatchCount.textContent =
                `${matchCount} of ${contactRows.length} rows match`;
        }
    }

    function resetClientTools() {
        if (filterColumn) {
            filterColumn.value = "all";
        }

        if (filterText) {
            filterText.value = "";
        }

        if (sortColumn) {
            sortColumn.value = "";
        }

        if (sortDirection) {
            sortDirection.value = "asc";
        }

        applyClientTools();
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
                new Date(
                    `${contact.dateOfBirth}T00:00:00`
                );

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

        const rawSalary =
            getInput(row, "salary")
                .value
                .trim();

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

            salary:
                rawSalary === ""
                    ? Number.NaN
                    : Number(rawSalary)
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
            applyClientTools();

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

    filterColumn?.addEventListener(
        "change",
        applyClientTools
    );

    filterText?.addEventListener(
        "input",
        applyClientTools
    );

    sortColumn?.addEventListener(
        "change",
        applyClientTools
    );

    sortDirection?.addEventListener(
        "change",
        applyClientTools
    );

    clearClientToolsButton?.addEventListener(
        "click",
        resetClientTools
    );

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

    applyClientTools();
});

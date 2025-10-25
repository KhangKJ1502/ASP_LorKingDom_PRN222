// === helpers ===
function afToken() {
    return $("#__afForm input[name='__RequestVerificationToken']").val();
}

// Focus trap đơn giản cho modal
let __prevActive = null;
function trapFocus(rootSelector) {
    __prevActive = document.activeElement;
    const selector = `${rootSelector} button, ${rootSelector} [href], ${rootSelector} input, ${rootSelector} select, ${rootSelector} textarea, ${rootSelector} [tabindex]:not([tabindex="-1"])`;
    const $els = $(selector).filter(":visible");
    $els.first().trigger("focus");

    $(document).on("keydown._trap", function (e) {
        if (e.key !== "Tab") return;
        const focusables = $(selector).filter(":visible");
        const first = focusables[0];
        const last = focusables[focusables.length - 1];
        if (e.shiftKey && document.activeElement === first) { e.preventDefault(); last.focus(); }
        else if (!e.shiftKey && document.activeElement === last) { e.preventDefault(); first.focus(); }
    });

    $(document).on("keydown._esc", function (e) {
        if (e.key === "Escape") {
            if (!$("#addressModal").hasClass("hidden")) hideAddressModal();
            if (!$("#confirmModal").hasClass("hidden")) hideConfirmModal();
        }
    });
}
function releaseFocus() {
    $(document).off("keydown._trap keydown._esc");
    if (__prevActive) { $(__prevActive).trigger("focus"); __prevActive = null; }
}

function showAddressModal() { $("#addressModal").removeClass("hidden"); trapFocus("#addressModal"); }
function hideAddressModal() { $("#addressModal").addClass("hidden"); releaseFocus(); }

function showConfirmModal() { $("#confirmModal").removeClass("hidden"); trapFocus("#confirmModal"); }
function hideConfirmModal() { $("#confirmModal").addClass("hidden"); releaseFocus(); }

// Open form modal (Add/Edit)
function openModal(edit = false, data = null) {
    $("#modalTitle").text(edit ? "Edit Address" : "Add Address");
    if (edit && data) {
        $("#addressId").val(data.addressId);
        $("#addressLine").val(data.addressLine);
        $("#city").val(data.city);
        $("#ward").val(data.ward || "");
        $("#isDefault").prop("checked", data.isDefault);
    } else {
        $("#addressId").val("");
        $("#addressLine").val("");
        $("#city").val("");
        $("#ward").val("");
        $("#isDefault").prop("checked", false);
    }
    showAddressModal();
}
function closeModal() { hideAddressModal(); }

// Confirm delete modal
let __deleteId = null;
function openConfirmDelete(id) { __deleteId = id; showConfirmModal(); }
function closeConfirmDelete() { __deleteId = null; hideConfirmModal(); }

// Render rows with beautiful buttons
function renderRows(items) {
    if (!items || items.length === 0) {
        $("#addressTableBody").html(
            `<tr><td colspan="5" class="p-4 text-center text-gray-500">No addresses found.</td></tr>`
        );
        return;
    }
    const rows = items.map(a => `
    <tr data-id="${a.addressId}" class="border-b last:border-b-0 hover:bg-orange-50/40">
      <td class="p-3">${a.addressLine}</td>
      <td class="p-3">${a.city}</td>
      <td class="p-3">${a.ward ?? "-"}</td>
      <td class="p-3">
        ${a.isDefault
            ? `<span class="inline-flex items-center gap-1 text-green-700 bg-green-50 border border-green-200 px-2 py-0.5 rounded-lg text-xs">
               <svg class="w-3.5 h-3.5" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                 <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"/>
               </svg>Default
             </span>`
            : `<span class="text-xs text-gray-500">—</span>`}
      </td>
      <td class="p-3 text-center">
        <div class="inline-flex items-center gap-2">
          <button class="edit-btn inline-flex items-center gap-1 px-3 py-1.5 rounded-lg border border-orange-200 text-orange-700 hover:bg-orange-50 transition">
            <svg class="w-4 h-4" viewBox="0 0 24 24" fill="none" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                    d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-8 1l8-8m0 0l-3-3m3 3L12 9"/>
            </svg>
            Edit
          </button>
          <button class="delete-btn inline-flex items-center gap-1 px-3 py-1.5 rounded-lg border border-red-200 text-red-700 hover:bg-red-50 transition">
            <svg class="w-4 h-4" viewBox="0 0 24 24" fill="none" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                    d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6M9 7h6m-7 0V6a2 2 0 012-2h2a2 2 0 012 2v1"/>
            </svg>
            Delete
          </button>
        </div>
      </td>
    </tr>
  `).join("");
    $("#addressTableBody").html(rows);
}

// AJAX partial + list
function loadPartial() {
    return $.get("/addresses/partial", html => { $("#profile-content").html(html); });
}
function loadList() { return $.getJSON("/addresses/list", renderRows); }

// === main ===
$(document).ready(function () {
    // Điều khiển tab: để addresses.js tự load partial + list khi tab = addresses
    $(document).on("click", ".profile-tab", async function () {
        $(".profile-tab").removeClass("active");
        $(this).addClass("active");
        const tab = $(this).data("tab");
        if (tab === "addresses") {
            await loadPartial();
            await loadList();
        }
    });

    // Open Add
    $(document).on("click", "#btnAddAddress", function () { openModal(false, null); });

    // Cancel/Close modal form
    $(document).on("click", "#cancelModal,[data-close-address]", closeModal);

    // Overlay click to close (optional)
    $("#addressModal").on("click", function (e) { if (e.target === this) closeModal(); });

    // Save (create / update)
    $(document).on("click", "#saveAddress", function () {
        const id = $("#addressId").val();
        const payload = {
            id: id || 0,
            addressLine: $("#addressLine").val().trim(),
            city: $("#city").val().trim(),
            ward: $("#ward").val().trim(),
            isDefault: $("#isDefault").is(":checked")
        };

        if (!payload.addressLine || !payload.city) {
            alert("Address Line và City là bắt buộc.");
            return;
        }

        const url = id ? "/addresses/update" : "/addresses/create";
        $.ajax({
            url,
            type: "POST",
            headers: { "RequestVerificationToken": afToken() },
            data: payload
        }).done(function () {
            closeModal();
            loadList();
        }).fail(function (xhr) {
            alert("Save failed: " + (xhr.responseText || "Unknown error"));
        });
    });

    // Edit (open form prefilled)
    $(document).on("click", ".edit-btn", function () {
        const $tr = $(this).closest("tr");
        const data = {
            addressId: parseInt($tr.data("id")),
            addressLine: $tr.children().eq(0).text().trim(),
            city: $tr.children().eq(1).text().trim(),
            ward: ($tr.children().eq(2).text().trim() === "-" ? "" : $tr.children().eq(2).text().trim()),
            // robust: kiểm tra tồn tại badge Default
            isDefault: $tr.find("td").eq(3).text().toLowerCase().includes("default")
        };
        openModal(true, data);
    });

    // Delete (open confirm dialog)
    $(document).on("click", ".delete-btn", function () {
        const id = $(this).closest("tr").data("id");
        openConfirmDelete(id);
    });

    // Confirm Delete modal bindings
    $(document).on("click", "[data-close-confirm],[data-cancel-confirm]", closeConfirmDelete);
    $("#confirmModal").on("click", function (e) { if (e.target === this) closeConfirmDelete(); });

    $(document).on("click", "#confirmDeleteBtn", function () {
        if (!__deleteId) return;
        $.ajax({
            url: "/addresses/delete",
            type: "POST",
            headers: { "RequestVerificationToken": afToken() },
            data: { id: __deleteId }
        }).done(function () {
            closeConfirmDelete();
            loadList();
        }).fail(function (xhr) {
            alert("Delete failed: " + (xhr.responseText || "Unknown error"));
        });
    });
});

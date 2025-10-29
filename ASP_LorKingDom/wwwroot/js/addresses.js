(function () {
    const modalEl = document.getElementById("addrModal");
    const modalBody = document.getElementById("addrModalBody");
    const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    const getToken = () => tokenInput?.value ?? "";
    let bsModal;

    /* ========= TOAST ========= */
    function ensureToastStyles() {
        if (document.getElementById("toast-style")) return;
        const css = `
      .toast { pointer-events:auto; display:flex; align-items:center; gap:.75rem;
        min-width:280px; max-width:420px; padding:.875rem 1rem; border-radius:12px;
        color:#fff; box-shadow:0 10px 24px rgba(0,0,0,.18); opacity:0; transform:translateY(-6px);
        transition:opacity .2s ease, transform .2s ease; font-weight:600; }
      .toast.show{opacity:1; transform:translateY(0)}
      .toast-error{background:#ef4444} .toast-success{background:#22c55e} .toast-info{background:#3b82f6}
      .toast-close{margin-left:auto; background:transparent; border:0; color:#fff; opacity:.85; font-size:16px}
      .toast-close:hover{opacity:1}
    `;
        const s = document.createElement("style");
        s.id = "toast-style";
        s.textContent = css;
        document.head.appendChild(s);
    }
    function showToast(type, message, timeout = 4000) {
        ensureToastStyles();
        const root = document.getElementById("toast-root") || (() => {
            const d = document.createElement("div");
            d.id = "toast-root";
            d.className = "fixed top-4 right-4 z-[9999] space-y-3 pointer-events-none";
            document.body.appendChild(d);
            return d;
        })();

        const div = document.createElement("div");
        div.className = `toast toast-${type}`;
        div.innerHTML = `
      <span>${message}</span>
      <button class="toast-close" aria-label="Close">✕</button>
    `;
        root.appendChild(div);
        requestAnimationFrame(() => div.classList.add("show"));

        const close = () => {
            div.classList.remove("show");
            setTimeout(() => div.remove(), 200);
        };
        div.querySelector(".toast-close").addEventListener("click", close);
        if (timeout > 0) setTimeout(close, timeout);
    }

    /* ========= FETCH HELPERS ========= */
    async function fetchHtmlOrToast(url, options, successMsg) {
        try {
            const res = await fetch(url, { credentials: "same-origin", ...options });
            const text = await res.text();
            if (!res.ok) {
                // server trả BadRequest/NotFound/500 với message text
                showToast("error", text || "Có lỗi xảy ra.");
                return null;
            }
            if (successMsg) showToast("success", successMsg);
            return text;
        } catch (err) {
            showToast("error", "Mất kết nối. Vui lòng thử lại.");
            return null;
        }
    }

    /* ========= MODAL / RENDER ========= */
    function ensureModal() {
        if (!bsModal) {
            bsModal = new bootstrap.Modal(modalEl, { backdrop: 'static', keyboard: true });
        }
    }

    async function openWithPartial(url, titleText) {
        ensureModal();
        document.getElementById("addrModalLabel").textContent = titleText || "Address";
        const html = await fetchHtmlOrToast(url);
        if (html) {
            modalBody.innerHTML = html;
            bsModal.show();
        }
    }

    function replaceListHtml(html) {
        const wrap = document.getElementById("addr-list-wrap");
        if (wrap) {
            if (html.includes('id="addr-list-wrap"')) {
                wrap.outerHTML = html;
            } else {
                wrap.innerHTML = html;
            }
        } else {
            (document.getElementById("profile-content") || document.body).innerHTML = html;
        }
    }

    async function postAndRefresh(action, formData, successMsg) {
        const resHtml = await fetchHtmlOrToast(action, {
            method: "POST",
            headers: { "RequestVerificationToken": getToken() },
            body: formData
        }, successMsg);

        if (resHtml) {
            replaceListHtml(resHtml);
            bsModal?.hide();
        }
    }

    /* ========= DELEGATION ========= */
    document.addEventListener("click", async (e) => {
        // ➕ Add
        if (e.target.closest(".addr-btn-add")) {
            e.preventDefault();
            await openWithPartial("/Address/FormPartialCreate", "Add Address");
            return;
        }

        // ✏️ Edit
        if (e.target.closest(".addr-btn-edit")) {
            e.preventDefault();
            const id = e.target.closest(".addr-btn-edit").dataset.id;
            await openWithPartial(`/Address/FormPartialEdit?id=${id}`, "Edit Address");
            return;
        }

        // 🗑️ Delete
        if (e.target.closest(".addr-btn-delete")) {
            e.preventDefault();
            const id = e.target.closest(".addr-btn-delete").dataset.id;
            const fd = new FormData(); fd.append("id", id);
            const html = await fetchHtmlOrToast("/Address/DeleteAjax", {
                method: "POST",
                headers: { "RequestVerificationToken": getToken() },
                body: fd
            }, "Xóa địa chỉ thành công.");
            if (html) replaceListHtml(html);
            return;
        }

        // ⭐ Set Default
        if (e.target.closest(".addr-btn-setdefault")) {
            e.preventDefault();
            const id = e.target.closest(".addr-btn-setdefault").dataset.id;
            const fd = new FormData(); fd.append("id", id);
            const html = await fetchHtmlOrToast("/Address/SetDefaultAjax", {
                method: "POST",
                headers: { "RequestVerificationToken": getToken() },
                body: fd
            }, "Đã đặt làm mặc định.");
            if (html) replaceListHtml(html);
            return;
        }

        // ❌ Cancel trong modal
        if (e.target.closest(".addr-form-cancel")) {
            e.preventDefault();
            bsModal?.hide();
            return;
        }
    });

    // 🧾 Submit form (Create/Edit) — CHẶN SUBMIT TRANG
    document.addEventListener("submit", async (e) => {
        const form = e.target;
        if (!form.matches("#addr-form-create, #addr-form-edit")) return;

        e.preventDefault();
        const fd = new FormData(form);
        const okMsg = form.id === "addr-form-create" ? "Thêm địa chỉ thành công." : "Cập nhật địa chỉ thành công.";
        await postAndRefresh(form.action, fd, okMsg);
    }, true);

    // 🔍 Search
    document.getElementById("addr-btn-search")?.addEventListener("click", async () => {
        const q = document.getElementById("addr-search")?.value || "";
        const html = await fetchHtmlOrToast(`/Address/ListPartial?q=${encodeURIComponent(q)}`);
        if (html) replaceListHtml(html);
    });
})();

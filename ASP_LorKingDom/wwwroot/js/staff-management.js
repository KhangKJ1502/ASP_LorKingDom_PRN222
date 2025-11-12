
if (typeof chrome !== 'undefined' && chrome.runtime) {
    chrome.runtime.onMessage?.addListener(() => true);
    void chrome.runtime.lastError;
}

const Config = {
    ALLOWED_TYPES: ['image/png', 'image/jpeg', 'image/jpg', 'image/webp', 'image/gif'],
    MAX_SIZE: 2 * 1024 * 1024, // 2MB
    ADD_PREFIX: 'add_',
    EDIT_PREFIX: 'edit_'
};

const $ = {
    get: (id) => document.getElementById(id),
    val: (id) => document.getElementById(id)?.value ?? '',
    set: (id, val) => { const el = document.getElementById(id); if (el) el.value = val; },
    check: (id, checked) => { const el = document.getElementById(id); if (el) el.checked = checked; },
    text: (id, text) => { const el = document.getElementById(id); if (el) el.textContent = text; },
    show: (id, visible = true) => { const el = document.getElementById(id); if (el) el.classList.toggle('d-none', !visible); }
};

const Toast = {
    show: (msg, isError = false) => {
        Swal.fire({
            icon: isError ? 'error' : 'success',
            title: msg,
            position: 'top-end',
            toast: true,
            timer: 3000,
            timerProgressBar: true,
            showConfirmButton: false,
            showClass: { popup: 'animate__animated animate__fadeInDown' },
            hideClass: { popup: 'animate__animated animate__fadeOutUp' }
        });
    },

    loading: () => {
        Swal.fire({
            title: '⏳ Đang xử lý...',
            allowOutsideClick: false,
            didOpen: () => Swal.showLoading()
        });
    },

    confirm: async (config) => {
        const result = await Swal.fire({
            ...config,
            showCancelButton: true,
            cancelButtonColor: '#6c757d',
            cancelButtonText: '<i class="fa-solid fa-xmark me-1"></i> Hủy',
            reverseButtons: true,
            showClass: { popup: 'animate__animated animate__zoomIn animate__faster' },
            hideClass: { popup: 'animate__animated animate__zoomOut animate__faster' }
        });
        return result.isConfirmed;
    }
};


const Avatar = {
    validate: (file) => {
        if (!Config.ALLOWED_TYPES.includes(file.type.toLowerCase())) {
            return 'Chỉ hỗ trợ PNG/JPG/WebP/GIF!';
        }
        if (file.size > Config.MAX_SIZE) {
            return 'Kích thước ảnh tối đa 2MB.';
        }
        return null;
    },

    preview: (file, prefix) => {
        const reader = new FileReader();
        reader.onload = (e) => {
            const img = $.get(`${prefix}avatarPreview`);
            const placeholder = $.get(`${prefix}avatarPlaceholder`);

            if (img) {
                img.src = e.target.result;
                img.classList.remove('d-none');
            }
            placeholder?.classList.add('d-none');
            $.set(`${prefix}removeImage`, 'false');
        };
        reader.readAsDataURL(file);
    },

    bind: (prefix) => {
        const input = $.get(`${prefix}avatarFile`);
        if (!input) return;

        input.addEventListener('change', function () {
            const file = this.files?.[0];
            if (!file) return;

            const error = Avatar.validate(file);
            if (error) {
                Toast.show(error, true);
                this.value = '';
                return;
            }

            Avatar.preview(file, prefix);
        });
    },

    clear: (prefix) => {
        const input = $.get(`${prefix}avatarFile`);
        const img = $.get(`${prefix}avatarPreview`);
        const placeholder = $.get(`${prefix}avatarPlaceholder`);

        if (input) input.value = '';
        if (img) {
            img.src = '';
            img.classList.add('d-none');
        }
        placeholder?.classList.remove('d-none');
        $.set(`${prefix}removeImage`, 'true');
    }
};

window.clearAvatar = (scope) => Avatar.clear(scope + '_');


const Modal = {
    resetAdd: () => {
        const form = $.get('addStaffForm');
        if (form) form.reset();

        $.check(`${Config.ADD_PREFIX}statusActive`, true);
        $.set(`${Config.ADD_PREFIX}status`, 'Active');
        $.set(`${Config.ADD_PREFIX}isDeleted`, 'false');

        Avatar.clear(Config.ADD_PREFIX);

        const placeholder = $.get(`${Config.ADD_PREFIX}avatarPlaceholder`);
        if (placeholder) {
            placeholder.textContent = 'S';
            placeholder.classList.remove('d-none');
        }
    },

    openAdd: () => {
        Modal.resetAdd();
        const modal = new bootstrap.Modal($.get('addStaffModal'));
        modal.show();
    },

    openEdit: (button) => {
        try {
            const staffJson = button.getAttribute('data-staff');
            if (!staffJson) throw new Error('No staff data');

            const data = JSON.parse(staffJson);
            Modal.fillEdit(data);

            const modal = new bootstrap.Modal($.get('editStaffModal'));
            modal.show();
        } catch (e) {
            console.error('❌ Parse error:', e);
            Toast.show('Lỗi tải dữ liệu!', true);
        }
    },

    fillEdit: (data) => {
        const p = Config.EDIT_PREFIX;

        // Basic fields
        $.set(`${p}id`, data.id || '');
        $.set(`${p}accountName`, data.name || '');
        $.set(`${p}email`, data.email || '');
        $.set(`${p}email_hidden`, data.email || '');
        $.set(`${p}phoneNumber`, data.phone || '');
        $.set(`${p}roleId`, data.roleid || '');

        // Status
        const status = data.isdeleted ? 'Inactive' : (data.status || 'Active');
        $.check(`${p}status${status}`, true);
        $.set(`${p}status`, status);
        $.set(`${p}isDeleted`, data.isdeleted ? 'true' : 'false');

        // Clear passwords
        $.set(`${p}newPassword`, '');
        $.set(`${p}confirmNewPassword`, '');

        // Dates
        const formatDate = (str) => {
            if (!str) return '—';
            try { return new Date(str).toLocaleString('vi-VN'); } catch { return '—'; }
        };
        $.text(`${p}createdAt`, formatDate(data.created));
        $.text(`${p}updatedAt`, formatDate(data.updated));

        // Avatar
        $.set(`${p}avatarFile`, '');
        $.set(`${p}removeImage`, 'false');

        const img = $.get(`${p}avatarPreview`);
        const placeholder = $.get(`${p}avatarPlaceholder`);

        if (data.image) {
            if (img) {
                img.src = data.image;
                img.classList.remove('d-none');
            }
            placeholder?.classList.add('d-none');
            $.set(`${p}existingImage`, data.image);
        } else {
            img?.classList.add('d-none');
            if (img) img.src = '';
            if (placeholder) {
                placeholder.textContent = (data.name || 'S')[0].toUpperCase();
                placeholder.classList.remove('d-none');
            }
            $.set(`${p}existingImage`, '');
        }
    },

    restoreAdd: (data) => {
        if (!data) return;

        const p = Config.ADD_PREFIX;
        const get = (key1, key2) => data?.[key1] ?? data?.[key2] ?? '';

        $.set(`${p}accountName`, get('accountName', 'AccountName'));
        $.set(`${p}phoneNumber`, get('phoneNumber', 'PhoneNumber'));
        $.set(`${p}roleId`, get('roleId', 'RoleId'));
        $.set(`${p}email`, get('email', 'Email'));

        const status = get('status', 'Status') || 'Active';
        const isDeleted = data?.isDeleted ?? data?.IsDeleted ?? false;

        if (isDeleted || status === 'Inactive') {
            $.check(`${p}statusInactive`, true);
            $.set(`${p}status`, 'Inactive');
            $.set(`${p}isDeleted`, 'true');
        } else {
            $.check(`${p}statusActive`, true);
            $.set(`${p}status`, 'Active');
            $.set(`${p}isDeleted`, 'false');
        }
        $.set(`${p}password`, '');
        $.set(`${p}confirmPassword`, '');
    },

    restoreEdit: (data) => {
        if (!data?.id && !data?.Id) return;

        const p = Config.EDIT_PREFIX;
        const get = (key1, key2) => data?.[key1] ?? data?.[key2] ?? '';

        $.set(`${p}id`, get('id', 'Id'));
        $.set(`${p}accountName`, get('accountName', 'AccountName'));
        $.set(`${p}email`, get('email', 'Email'));
        $.set(`${p}phoneNumber`, get('phoneNumber', 'PhoneNumber'));
        $.set(`${p}roleId`, get('roleId', 'RoleId'));

        const status = get('status', 'Status') || 'Active';
        const isDeleted = data?.isDeleted ?? data?.IsDeleted ?? false;

        if (isDeleted || status === 'Inactive') {
            $.check(`${p}statusInactive`, true);
            $.set(`${p}status`, 'Inactive');
            $.set(`${p}isDeleted`, 'true');
        } else {
            $.check(`${p}statusActive`, true);
            $.set(`${p}status`, 'Active');
            $.set(`${p}isDeleted`, 'false');
        }

        // Security: Don't restore passwords
        $.set(`${p}newPassword`, '');
        $.set(`${p}confirmNewPassword`, '');

        // Restore avatar if exists
        const image = get('image', 'Image');
        if (image) {
            const img = $.get(`${p}avatarPreview`);
            const placeholder = $.get(`${p}avatarPlaceholder`);
            if (img) {
                img.src = image;
                img.classList.remove('d-none');
            }
            placeholder?.classList.add('d-none');
            $.set(`${p}existingImage`, image);
            $.set(`${p}removeImage`, 'false');
        }
    }
};

window.openAddModal = () => Modal.openAdd();
window.openEditModal = (btn) => Modal.openEdit(btn);


window.confirmDeleteStaff = async (e) => {
    e.preventDefault();
    const ok = await Toast.confirm({
        icon: 'warning',
        title: 'Vô hiệu hóa nhân viên?',
        html: '<p>Nhân viên sẽ không thể đăng nhập.</p>',
        confirmButtonColor: '#ef4444',
        confirmButtonText: '<i class="fa-solid fa-ban me-1"></i> Vô hiệu hóa'
    });

    if (ok) {
        Toast.loading();
        e.target.submit();
    }
    return false;
};

window.confirmRestoreStaff = async (e) => {
    e.preventDefault();
    const ok = await Toast.confirm({
        icon: 'question',
        title: 'Khôi phục nhân viên?',
        html: '<p>Tài khoản sẽ được kích hoạt lại.</p>',
        confirmButtonColor: '#22c55e',
        confirmButtonText: '<i class="fa-solid fa-check-circle me-1"></i> Khôi phục'
    });

    if (ok) {
        Toast.loading();
        e.target.submit();
    }
    return false;
};


window.changePageSize = (size) => {
    const query = document.body.dataset.query ?? '';
    const action = query ? 'Search' : 'Index';
    const params = new URLSearchParams({ page: 1, pageSize: size });
    if (query) params.append('q', query);

    const base = document.body.dataset.baseUrl ?? '/AccountStaff';
    window.location.href = `${base}/${action}?${params}`;
};

window.setStatus = (scope, status) => {
    $.set(`${scope}_status`, status);
    $.set(`${scope}_isDeleted`, status === 'Inactive' ? 'true' : 'false');
};
document.addEventListener('DOMContentLoaded', function () {
    console.log('Staff Management loaded');

    // 1) Show messages from TempData
    const msgError = document.body.dataset.msgError ?? '';
    const msgSuccess = document.body.dataset.msgSuccess ?? '';
    const msgViewBag = document.body.dataset.msgViewBagError ?? '';

    try {
        if (msgError) {
            const parsed = JSON.parse(msgError);
            if (parsed) Toast.show(parsed, true);
        }
    } catch {
        if (msgError) Toast.show(msgError, true);
    }

    try {
        if (msgSuccess) {
            const parsed = JSON.parse(msgSuccess);
            if (parsed) Toast.show(parsed, false);
        }
    } catch {
        if (msgSuccess) Toast.show(msgSuccess, false);
    }

    try {
        if (msgViewBag) {
            const parsed = JSON.parse(msgViewBag);
            if (parsed) Toast.show(parsed, true);
        }
    } catch {
        if (msgViewBag) Toast.show(msgViewBag, true);
    }

    // 2) Bind avatar handlers
    Avatar.bind(Config.ADD_PREFIX);
    Avatar.bind(Config.EDIT_PREFIX);

    // 3) Restore forms on validation errors
    const showAdd = (document.body.dataset.showAddModal ?? 'false') === 'true';
    const hasEdit = (document.body.dataset.hasEditModal ?? 'false') === 'true';

    if (showAdd) {
        try {
            const addData = JSON.parse(document.body.dataset.addStaffModel ?? '{}');
            Modal.restoreAdd(addData);
            const modal = new bootstrap.Modal($.get('addStaffModal'));
            modal.show();
        } catch (e) {
            console.error('❌ Restore add error:', e);
        }
    }

    if (hasEdit) {
        try {
            const editData = JSON.parse(document.body.dataset.editStaffModel ?? '{}');
            const get = (k1, k2) => editData?.[k1] ?? editData?.[k2] ?? '';

            Modal.fillEdit({
                id: get('id', 'Id') || 0,
                name: get('accountName', 'AccountName'),
                phone: get('phoneNumber', 'PhoneNumber'),
                roleid: String(get('roleId', 'RoleId') || ''),
                email: get('email', 'Email'),
                image: get('image', 'Image'),
                status: get('status', 'Status') || 'Active',
                isdeleted: !!(editData?.isDeleted ?? editData?.IsDeleted),
                created: get('createdAt', 'CreatedAt'),
                updated: get('updatedAt', 'UpdatedAt')
            });

            const modal = new bootstrap.Modal($.get('editStaffModal'));
            modal.show();
        } catch (e) {
            console.error('❌ Restore edit error:', e);
        }
    }

    console.log('Staff Management ready');
});
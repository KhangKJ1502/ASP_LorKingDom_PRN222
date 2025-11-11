/* 
 * Click nbfs://nbhost/SystemFileSystem/Templates/Licenses/license-default.txt to change this license
 * Click nbfs://nbhost/SystemFileSystem/Templates/JSP_Servlet/JavaScript.js to edit this template
 */

// Initialize Quill Editor
const quill = new Quill('#editor-container', {
    theme: 'snow',
    modules: {
        toolbar: [
            ['bold', 'italic', 'underline', 'strike'], // In đậm, nghiêng, gạch chân, gạch ngang
            [{ 'header': [1, 2, 3, false] }], // Tiêu đề (Heading)
            [{ 'list': 'ordered' }, { 'list': 'bullet' }], // Danh sách có thứ tự và không thứ tự
            [{ 'script': 'sub' }, { 'script': 'super' }], // Chỉ số trên, chỉ số dưới
            [{ 'indent': '-1' }, { 'indent': '+1' }], // Thụt lề trái, phải
            [{ 'direction': 'rtl' }], // Hướng văn bản (trái sang phải, phải sang trái)
            [{ 'size': ['small', false, 'large', 'huge'] }], // Kích thước chữ
            [{ 'header': [1, 2, 3, 4, 5, 6, false] }], // Cỡ tiêu đề
            [{ 'color': [] }, { 'background': [] }], // Màu chữ, màu nền
            [{ 'font': [] }], // Font chữ
            [{ 'align': [] }], // Căn chỉnh văn bản
            ['link'], // Chèn link
            ['clean']                                         // Xóa định dạng
        ]
    },
    placeholder: 'Describe your product in detail...'

});
var form = document.querySelector('form');
form.onsubmit = function () {
    var descriptionInput = document.querySelector('input[name="description"]');
    descriptionInput.value = quill.root.innerHTML;
};

document.getElementById('mainImageUpload').addEventListener('change', function (e) {
    const file = e.target.files[0];
    const previewContainer = document.getElementById('previewContainer');

    previewContainer.innerHTML = '';

    if (file) {
        const reader = new FileReader();
        reader.onload = (event) => {
            const wrapper = document.createElement('div');
            wrapper.className = 'preview-wrapper';

            const img = document.createElement('img');
            img.className = 'preview-image';
            img.src = event.target.result;

            const deleteBtn = document.createElement('button');
            deleteBtn.className = 'delete-btn';
            deleteBtn.innerHTML = '×';
            deleteBtn.onclick = () => {
                previewContainer.innerHTML = '';
                e.target.value = '';
            };

            wrapper.appendChild(img);
            wrapper.appendChild(deleteBtn);
            previewContainer.appendChild(wrapper);
        };
        reader.readAsDataURL(file);
    }
});
function createDeleteButton(onClick) {
    const deleteBtn = document.createElement('div');
    deleteBtn.className = 'delete-btn';
    deleteBtn.innerHTML = '×';
    deleteBtn.onclick = onClick;
    return deleteBtn;
}

//TJs Them Anh chi tiet
// JavaScript
const MAX_IMAGES = 6; // set số ảnh được update detail
let currentFiles = [];

function handleFileSelect(event) {
    const files = event.target.files || event.dataTransfer.files;
    processFiles(files);
}

function handleDragOver(event) {
    event.preventDefault();
    event.stopPropagation();
    event.target.classList.add('dragover');
}

function handleDrop(event) {
    event.preventDefault();
    event.stopPropagation();
    event.target.classList.remove('dragover');
    handleFileSelect(event);
}

function processFiles(files) {
    const validFiles = Array.from(files).filter(file => {
        return file.type.startsWith('image/') &&
            ['image/jpeg', 'image/png', 'image/webp'].includes(file.type);
    });

    if (validFiles.length === 0) {
        showError('Please upload only JPEG/PNG images');
        return;
    }

    if (currentFiles.length + validFiles.length > MAX_IMAGES) {
        showError(`Maximum ${MAX_IMAGES} images allowed`);
        return;
    }

    currentFiles = [...currentFiles, ...validFiles].slice(0, MAX_IMAGES);
    updatePreview();
    updateFileInput();
}

function updateFileInput() {
    const dataTransfer = new DataTransfer();
    currentFiles.forEach(file => dataTransfer.items.add(file));
    document.getElementById('detailImagesInput').files = dataTransfer.files;
}

function updatePreview() {
    const previewGrid = document.getElementById('previewGrid');
    previewGrid.innerHTML = '';

    currentFiles.forEach((file, index) => {
        const reader = new FileReader();
        reader.onload = (e) => {
            const previewItem = document.createElement('div');
            previewItem.className = 'preview-item';

            previewItem.innerHTML = `
                <img src="${e.target.result}" class="preview-image">
                <button 
                    type="button" 
                    class="delete-btn" 
                    onclick="removeImage(${index})"
                >&times;</button>
            `;

            previewGrid.appendChild(previewItem);
        };
        reader.readAsDataURL(file);
    });
}

function removeImage(index) {
    currentFiles = currentFiles.filter((_, i) => i !== index);
    updatePreview();
    updateFileInput();
}

function showError(message) {
    const errorDiv = document.createElement('div');
    errorDiv.className = 'error-message';
    errorDiv.textContent = message;

    const container = document.querySelector('.mess-error');
    const existingError = container.querySelector('.error-message');
    if (existingError)
        existingError.remove();

    container.appendChild(errorDiv);
    setTimeout(() => errorDiv.remove(), 3000);
}



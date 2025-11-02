// Open review modal
window.openReviewModal = async function (productId, productName, imageUrl) {
    try {
        console.log('Opening review modal for product:', productId);

        // Get product data
        const response = await fetch(`/Review/GetReviewData?productId=${productId}`, {
            method: 'GET',
            credentials: 'same-origin'
        });

        const data = await response.json();
        console.log('Review data response:', data);

        if (!data.success) {
            throw new Error(data.error || 'Không thể tải thông tin sản phẩm');
        }

        // Fill modal data
        document.getElementById('reviewProductId').value = productId;
        document.getElementById('reviewProductName').textContent = productName || data.productName;
        document.getElementById('reviewProductImage').src = imageUrl || data.mainImageUrl || 'https://via.placeholder.com/80';

        // Reset form
        document.getElementById('reviewForm').reset();
        document.getElementById('ratingValue').value = '0';
        document.getElementById('imagePreview').innerHTML = '';

        // Clear stars
        document.querySelectorAll('#starRating i').forEach(star => {
            star.classList.remove('fas', 'active');
            star.classList.add('far');
        });

        // Show modal
        const modal = new bootstrap.Modal(document.getElementById('reviewModal'));
        modal.show();
    } catch (error) {
        console.error('Error opening review modal:', error);
        Swal.fire({
            icon: 'error',
            title: 'Lỗi',
            text: error.message
        });
    }
};

// Submit review
window.submitReview = async function () {
    try {
        const productId = document.getElementById('reviewProductId').value;
        const rating = document.getElementById('ratingValue').value;
        const comment = document.getElementById('reviewComment').value.trim();
        const images = document.getElementById('reviewImages').files;

        console.log('Submitting review:', { productId, rating, comment, imageCount: images.length });

        // Validation
        if (!rating || rating == '0') {
            Swal.fire({
                icon: 'warning',
                title: 'Thiếu thông tin',
                text: 'Vui lòng chọn số sao đánh giá'
            });
            return;
        }

        if (!comment || comment.length < 10) {
            Swal.fire({
                icon: 'warning',
                title: 'Thiếu thông tin',
                text: 'Vui lòng nhập ít nhất 10 ký tự cho nhận xét'
            });
            return;
        }

        if (images.length > 5) {
            Swal.fire({
                icon: 'warning',
                title: 'Quá nhiều hình ảnh',
                text: 'Chỉ được tải lên tối đa 5 hình ảnh'
            });
            return;
        }

        // Show loading
        Swal.fire({
            title: 'Đang gửi đánh giá...',
            allowOutsideClick: false,
            didOpen: () => Swal.showLoading()
        });

        // Prepare form data
        const formData = new FormData();
        formData.append('ProductId', productId);
        formData.append('Rating', rating);
        formData.append('Comment', comment);

        // Add images
        for (let i = 0; i < images.length; i++) {
            formData.append('Images', images[i]);
        }

        // Get token
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
        if (token) {
            formData.append('__RequestVerificationToken', token);
        }

        // Submit
        const response = await fetch('/Review/Create', {
            method: 'POST',
            body: formData,
            credentials: 'same-origin'
        });

        const data = await response.json();
        console.log('Create review response:', data);

        if (data.success) {
            // Close modal
            const modalElement = document.getElementById('reviewModal');
            const modal = bootstrap.Modal.getInstance(modalElement);
            if (modal) {
                modal.hide();
            }

            // Show success message
            Swal.fire({
                icon: 'success',
                title: 'Thành công!',
                text: data.message,
                timer: 2000,
                showConfirmButton: false
            }).then(() => {
                // Reload current page or redirect to reviews tab
                if (window.location.href.includes('/Home/Profile')) {
                    // Navigate to reviews tab
                    window.location.hash = 'reviews';
                    window.location.reload();
                } else {
                    // Reload current page
                    window.location.reload();
                }
            });
        } else {
            throw new Error(data.error || 'Có lỗi xảy ra khi gửi đánh giá');
        }
    } catch (error) {
        console.error('Error submitting review:', error);
        Swal.fire({
            icon: 'error',
            title: 'Lỗi',
            text: error.message
        });
    }
};
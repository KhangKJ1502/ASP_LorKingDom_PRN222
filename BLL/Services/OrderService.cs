using BLL.DTOs;
using BLL.Interfaces;
using DAL.Interfaces;
using DAL.Models;

namespace BLL.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepo;
        private readonly ICartRepository _cartRepo;
        private readonly IProductRepository _productRepo;
        private readonly IVoucherRepository _voucherRepo;
        private readonly IAddressRepository _addressRepo;
        private readonly IWalletRepository _walletRepo;
        private readonly IWalletTransactionRepository _walletTransactionRepo;

        public OrderService(
            IOrderRepository orderRepo,
            ICartRepository cartRepo,
            IProductRepository productRepo,
            IVoucherRepository voucherRepo,
            IAddressRepository addressRepo,
            IWalletRepository walletRepo,
            IWalletTransactionRepository walletTransactionRepo)
        {
            _orderRepo = orderRepo;
            _cartRepo = cartRepo;
            _productRepo = productRepo;
            _voucherRepo = voucherRepo;
            _addressRepo = addressRepo;
            _walletRepo = walletRepo;
            _walletTransactionRepo = walletTransactionRepo;
        }

        public async Task<(bool success, string message, int? orderId)> CreateOrderAsync(int accountId, CheckoutDto checkoutDto)
        {
            try
            {
                // 1. Validate input
                if (string.IsNullOrWhiteSpace(checkoutDto.FullName) ||
                    string.IsNullOrWhiteSpace(checkoutDto.Email) ||
                    string.IsNullOrWhiteSpace(checkoutDto.Phone))
                {
                    return (false, "Vui lòng nhập đầy đủ thông tin cá nhân", null);
                }

                // 2. Get cart items
                var cart = await _cartRepo.GetByAccountIdAsync(accountId);
                if (cart == null)
                {
                    return (false, "Giỏ hàng của bạn đang trống", null);
                }

                // 3. Calculate amounts:
                // - totalAmountOriginal: Tổng giá gốc (không sale) - lưu vào DB
                // - subtotalAfterSale: Tổng giá sau sale - dùng để tính voucher và hiển thị
                decimal totalAmountOriginal = 0;  // Giá gốc không sale
                decimal subtotalAfterSale = 0;     // Giá sau sale

                foreach (var item in cart?.CartItems ?? new List<CartItem>())
                {
                    if (item.Product.IsDeleted || item.Quantity > item.Product.Quantity)
                    {
                        return (false, $"Sản phẩm '{item.Product.ProductName}' hết hàng", null);
                    }
                    // Giá gốc (không sale)
                    totalAmountOriginal += item.Quantity * item.Product.Price;
                    // Giá sau sale (PriceAtThatTime đã bao gồm promotion)
                    subtotalAfterSale += item.Quantity * item.PriceAtThatTime;
                }

                // 4. Handle address - Only validate if all address fields are provided or all are empty
                string shippingAddressLine;
                string shippingCity;
                string shippingWard;

                bool hasStreet = !string.IsNullOrWhiteSpace(checkoutDto.Street);
                bool hasCity = !string.IsNullOrWhiteSpace(checkoutDto.City);
                bool hasDistrict = !string.IsNullOrWhiteSpace(checkoutDto.District);

                // If any address field is provided, all must be provided
                if (hasStreet || hasCity || hasDistrict)
                {
                    if (!hasStreet || !hasCity || !hasDistrict)
                    {
                        return (false, "Vui lòng nhập đầy đủ địa chỉ", null);
                    }

                    shippingAddressLine = checkoutDto.Street!;
                    shippingCity = checkoutDto.City!;
                    shippingWard = checkoutDto.District!;
                }
                else
                {
                    // No address fields provided - this should not happen if frontend validation works
                    return (false, "Vui lòng chọn hoặc nhập địa chỉ giao hàng", null);
                }

                // 5. Calculate shipping fee
                decimal shippingFee = checkoutDto.ShippingMethod == "express" ? 40000 : 20000;

                // 6. Handle promo code
                decimal discount = 0;
                int? voucherId = null;
                if (!string.IsNullOrWhiteSpace(checkoutDto.PromoCode))
                {
                    var voucher = await _voucherRepo.GetByCodeAsync(checkoutDto.PromoCode);
                    if (voucher == null)
                    {
                        return (false, "Mã giảm giá không hợp lệ!", null);
                    }

                    voucherId = voucher.VoucherId;

                    // Kiểm tra thời hạn
                    if (DateTime.Now < voucher.StartDate || DateTime.Now > voucher.EndDate)
                    {
                        return (false, "Mã giảm giá đã hết hạn", null);
                    }
                    // Kiểm tra nếu voucher có giới hạn dùng mỗi tài khoản
                    if (voucher.UsageLimitPerUser.HasValue && voucher.UsageLimitPerUser.Value > 0)
                    {
                        var userUsedCount = await _orderRepo.CountByVoucherAndAccountAsync(voucher.VoucherId, accountId);
                        if (userUsedCount >= voucher.UsageLimitPerUser.Value)
                        {
                            return (false, "Bạn đã sử dụng mã này trước đó", null);
                        }
                    }
                    // Kiểm tra min order (dựa trên giá sau sale)
                    if (voucher.MinOrderAmount.HasValue && subtotalAfterSale < voucher.MinOrderAmount.Value)
                    {
                        return (false, $"Đơn hàng phải từ {voucher.MinOrderAmount.Value:N0}₫ mới dùng được mã này", null);
                    }

                    // 🎯 TÍNH DISCOUNT DỰA VÀO VOUCHER TYPE (tính trên giá sau sale)
                    var voucherType = voucher.VoucherType?.VoucherTypeName?.ToLower() ?? "";
                    if (voucherType.Contains("percent") || voucherType.Contains("%"))
                    {
                        // Voucher phần trăm
                        discount = subtotalAfterSale * voucher.DiscountValue / 100;
                        // Áp dụng max discount nếu có
                        if (voucher.MaxDiscountAmount.HasValue && discount > voucher.MaxDiscountAmount.Value)
                        {
                            discount = voucher.MaxDiscountAmount.Value;
                        }
                    }
                    else
                    {
                        // Voucher fixed amount
                        discount = voucher.DiscountValue;
                    }
                }

                // 7. Calculate final amount for payment (not saved to DB)
                // finalAmount = subtotalAfterSale + shipping - voucher
                decimal finalAmount = subtotalAfterSale + shippingFee - discount;
                finalAmount = Math.Max(finalAmount, 0);

                // 8. Handle wallet payment
                decimal paidByWallet = 0;
                decimal paidByExternal = finalAmount;
                Wallet? wallet = null;

                if (checkoutDto.PaymentMethod?.ToLower() == "wallet")
                {
                    // Get wallet
                    wallet = await _walletRepo.GetByAccountIdAsync(accountId);
                    if (wallet == null)
                    {
                        return (false, "Bạn chưa có ví để thanh toán", null);
                    }

                    if (wallet.Status != "Active")
                    {
                        return (false, "Ví của bạn đang bị khóa", null);
                    }

                    if (wallet.Balance < finalAmount)
                    {
                        return (false, $"Số dư ví không đủ. Số dư hiện tại: {wallet.Balance:N0} ₫", null);
                    }

                    // Deduct from wallet
                    paidByWallet = finalAmount;
                    paidByExternal = 0;

                    wallet.Balance -= finalAmount;
                    wallet.LastTransactionAt = DateTime.UtcNow;
                    wallet.UpdatedAt = DateTime.UtcNow;
                    await _walletRepo.UpdateAsync(wallet);
                }

                // 9. Create order
                var order = new Order
                {
                    AccountId = accountId,
                    VoucherId = voucherId,
                    StatusId = 1, // Pending
                    ShippingName = checkoutDto.FullName,
                    ShippingPhone = checkoutDto.Phone,
                    ShippingAddressLine = shippingAddressLine,
                    ShippingCity = shippingCity,
                    ShippingWard = shippingWard,
                    ShippingMethod = checkoutDto.ShippingMethod,
                    OrderDate = DateTime.UtcNow,
                    TotalAmount = totalAmountOriginal,  // Lưu giá gốc (không sale)
                    PaidByWalletAmount = paidByWallet,
                    PaidByExternalAmount = paidByExternal,
                    RefundStatus = "None",
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow
                };

                // 10. Create order details
                foreach (var cartItem in cart?.CartItems ?? new List<CartItem>())
                {
                    // Use PriceAtThatTime from cart (which already includes promotion discount)
                    var orderDetail = new OrderDetail
                    {
                        Order = order,
                        ProductId = cartItem.ProductId,
                        Quantity = cartItem.Quantity,
                        UnitPrice = cartItem.PriceAtThatTime,
                        Discount = 0,
                        Total = cartItem.Quantity * cartItem.PriceAtThatTime,
                        IsDeleted = false,
                        Reviewed = false,
                        CreatedAt = DateTime.UtcNow
                    };
                    order.OrderDetails.Add(orderDetail);
                }

                // 11. Save order to database
                int orderId = await _orderRepo.CreateOrderAsync(order);

                // 12. Create wallet transaction if paid by wallet
                if (paidByWallet > 0 && wallet != null)
                {
                    var transaction = new WalletTransaction
                    {
                        WalletId = wallet.WalletId,
                        AccountId = accountId,
                        RelatedOrderId = orderId,
                        TxnType = "Payment",
                        Direction = "DR", // Debit 
                        Amount = paidByWallet,
                        BalanceBefore = wallet.Balance + paidByWallet,
                        BalanceAfter = wallet.Balance,
                        Method = "Wallet",
                        Status = "Completed",
                        Reason = $"Thanh toán đơn hàng #{orderId}",
                        IdempotencyKey = Guid.NewGuid().ToString(),
                        CreatedAt = DateTime.UtcNow,
                        CompletedAt = DateTime.UtcNow
                    };
                    await _walletTransactionRepo.AddAsync(transaction);
                    await _walletRepo.SaveChangesAsync();
                }

                // 13. Update product stock
                foreach (var cartItem in cart?.CartItems ?? new List<CartItem>())
                {
                    var product = cartItem.Product;
                    product.Quantity -= cartItem.Quantity;
                    await _productRepo.UpdateAsync(product);
                }

                // 14. Clear cart
                await _cartRepo.ClearCartByAccountIdAsync(accountId);

                return (true, "Đơn hàng được tạo thành công!", orderId);
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi: {ex.Message}", null);
            }
        }

        public async Task<List<OrderDto>> GetOrdersByAccountIdAsync(int accountId)
        {
            var orders = await _orderRepo.GetOrdersByAccountIdAsync(accountId);
            return orders.Select(MapToDto).ToList();
        }

        public async Task<OrderDto?> GetOrderByIdAsync(int orderId)
        {
            var order = await _orderRepo.GetOrderByIdAsync(orderId);
            return order != null ? MapToDto(order) : null;
        }

        public async Task<PagedResult<OrderDto>> GetAllOrdersAsync(string? query, int? statusId, DateTime? dateFrom, DateTime? dateTo, int page, int pageSize)
        {
            var skip = (page - 1) * pageSize;
            var (orders, totalCount) = await _orderRepo.GetAllOrdersAsync(query, statusId, dateFrom, dateTo, skip, pageSize);

            var orderDtos = orders.Select(MapToDto).ToList();

            return new PagedResult<OrderDto>
            {
                Items = orderDtos,
                Total = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<bool> UpdateOrderStatusAsync(int orderId, int newStatusId)
        {
            return await _orderRepo.UpdateOrderStatusAsync(orderId, newStatusId);
        }

        public async Task<bool> UpdateOrderStatusAsync(int orderId, int newStatusId, int? changedBy, string? note = null)
        {
            return await _orderRepo.UpdateOrderStatusAsync(orderId, newStatusId, changedBy, note);
        }

        private OrderDto MapToDto(Order order)
        {
            // Tính tổng tiền sản phẩm sau sale (từ OrderDetails)
            var subtotalAfterSale = order.OrderDetails?.Sum(od => od.Total ?? 0) ?? 0;

            // Xác định phí vận chuyển dựa trên ShippingMethod
            decimal shippingFee = 0;
            if (order.ShippingMethod?.ToLower() == "standard")
                shippingFee = 20000;
            else if (order.ShippingMethod?.ToLower() == "express")
                shippingFee = 40000;

            // Tính discount amount từ voucher
            // Formula: subtotalAfterSale + shippingFee - discountAmount - walletAmount = paidByExternal
            // => discountAmount = subtotalAfterSale + shippingFee - walletAmount - paidByExternal
            var discountAmount = subtotalAfterSale + shippingFee - order.PaidByWalletAmount - order.PaidByExternalAmount;
            discountAmount = Math.Max(0, discountAmount); // Không cho âm

            return new OrderDto
            {
                OrderId = order.OrderId,
                AccountId = order.AccountId,
                AccountName = order.Account?.AccountName,
                VoucherId = order.VoucherId,
                VoucherCode = order.Voucher?.VoucherCode,
                StatusId = order.StatusId,
                StatusName = order.Status?.StatusName,
                ShippingName = order.ShippingName ?? string.Empty,
                ShippingPhone = order.ShippingPhone ?? string.Empty,
                ShippingAddressLine = order.ShippingAddressLine ?? string.Empty,
                ShippingCity = order.ShippingCity ?? string.Empty,
                ShippingWard = order.ShippingWard ?? string.Empty,
                ShippingMethod = order.ShippingMethod,
                ShippingFee = shippingFee,
                DiscountAmount = discountAmount,
                OrderDate = order.OrderDate,
                TotalAmount = order.TotalAmount,  // Giá gốc không sale
                PaidByWalletAmount = order.PaidByWalletAmount,
                PaidByExternalAmount = order.PaidByExternalAmount,
                RefundStatus = order.RefundStatus,
                IsDeleted = order.IsDeleted,
                CreatedAt = order.CreatedAt,
                // Thông tin OrderRefund (nếu có)
                OrderRefundStatus = order.OrderRefund?.RefundStatus,
                OrderRefundId = order.OrderRefund?.RefundId,
                OrderDetails = order.OrderDetails?.Select(od => new OrderDetailDto
                {
                    OrderDetailId = od.OrderDetailId,
                    OrderId = od.OrderId,
                    ProductId = od.ProductId,
                    ProductName = od.Product?.ProductName,
                    MainImageUrl = od.Product?.ProductImages?.FirstOrDefault()?.ImageUrl,
                    Quantity = od.Quantity,
                    UnitPrice = od.UnitPrice,
                    Discount = od.Discount,
                    Total = od.Total ?? 0,
                    Reviewed = od.Reviewed,
                    IsDeleted = od.IsDeleted
                }).ToList() ?? new List<OrderDetailDto>()
            };
        }

        public async Task<List<PendingReviewDto>> GetPendingReviewProductsAsync(int accountId)
        {
            var orders = await _orderRepo.GetByAccountIdAsync(accountId);

            var deliveredOrders = orders
                .Where(o => o.StatusId == 4 && !o.IsDeleted) // Status 4 = Delivered
                .ToList();

            var pendingReviews = new List<PendingReviewDto>();

            foreach (var order in deliveredOrders)
            {
                var orderDetails = await _orderRepo.GetOrderDetailsByOrderIdAsync(order.OrderId);

                foreach (var detail in orderDetails.Where(d => !d.Reviewed && !d.IsDeleted))
                {
                    var product = await _productRepo.GetByIdAsync(detail.ProductId);
                    if (product != null && !product.IsDeleted)
                    {
                        pendingReviews.Add(new PendingReviewDto
                        {
                            ProductId = product.ProductId,
                            ProductName = product.ProductName,
                            MainImageUrl = product.ProductImages?.FirstOrDefault(i => i.IsMain)?.ImageUrl,
                            PurchaseDate = order.OrderDate,
                            OrderId = order.OrderId,
                            Price = detail.UnitPrice
                        });
                    }
                }
            }

            return pendingReviews.OrderByDescending(p => p.PurchaseDate).ToList();
        }

        public async Task<bool> CanReviewProductAsync(int accountId, int productId)
        {
            var orders = await _orderRepo.GetByAccountIdAsync(accountId);

            var hasDeliveredOrder = orders.Any(o =>
                o.StatusId == 4 && // Delivered
                !o.IsDeleted &&
                o.OrderDetails.Any(od => od.ProductId == productId && !od.IsDeleted)
            );

            return hasDeliveredOrder;
        }

        public async Task MarkProductAsReviewedAsync(int accountId, int productId)
        {
            var orders = await _orderRepo.GetByAccountIdAsync(accountId);

            foreach (var order in orders.Where(o => o.StatusId == 4 && !o.IsDeleted))
            {
                var orderDetails = await _orderRepo.GetOrderDetailsByOrderIdAsync(order.OrderId);
                var detail = orderDetails.FirstOrDefault(od => od.ProductId == productId && !od.IsDeleted);

                if (detail != null)
                {
                    detail.Reviewed = true;
                    await _orderRepo.UpdateOrderDetailAsync(detail);
                }
            }
        }

        public async Task UnmarkProductAsReviewedAsync(int accountId, int productId)
        {
            var orders = await _orderRepo.GetByAccountIdAsync(accountId);

            foreach (var order in orders.Where(o => o.StatusId == 4 && !o.IsDeleted))
            {
                var orderDetails = await _orderRepo.GetOrderDetailsByOrderIdAsync(order.OrderId);
                var detail = orderDetails.FirstOrDefault(od => od.ProductId == productId && !od.IsDeleted);

                if (detail != null)
                {
                    detail.Reviewed = false;
                    await _orderRepo.UpdateOrderDetailAsync(detail);
                }
            }
        }
    }
}
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

        public OrderService(
            IOrderRepository orderRepo,
            ICartRepository cartRepo,
            IProductRepository productRepo,
            IVoucherRepository voucherRepo,
            IAddressRepository addressRepo)
        {
            _orderRepo = orderRepo;
            _cartRepo = cartRepo;
            _productRepo = productRepo;
            _voucherRepo = voucherRepo;
            _addressRepo = addressRepo;
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

                // 3. Validate stock & calculate total
                decimal totalAmount = 0;
                foreach (var item in cart?.CartItems ?? new List<CartItem>())
                {
                    if (item.Product.IsDeleted || item.Quantity > item.Product.Quantity)
                    {
                        return (false, $"Sản phẩm '{item.Product.ProductName}' hết hàng", null);
                    }
                    totalAmount += item.Quantity * item.Product.Price;
                }

                // 4. Handle address
                if (string.IsNullOrWhiteSpace(checkoutDto.Street) ||
                    string.IsNullOrWhiteSpace(checkoutDto.City) ||
                    string.IsNullOrWhiteSpace(checkoutDto.District))
                {
                    return (false, "Vui lòng nhập đầy đủ địa chỉ mới", null);
                }
                string shippingAddressLine = checkoutDto.Street;
                string shippingCity = checkoutDto.City;
                string shippingWard = checkoutDto.District;

                // 5. Calculate shipping fee
                decimal shippingFee = checkoutDto.ShippingMethod == "express" ? 40000 : 20000;
                if (totalAmount > 500000) shippingFee = 0;

                // 6. Handle promo code
                decimal discount = 0;
                int? voucherId = null;
                if (!string.IsNullOrWhiteSpace(checkoutDto.PromoCode))
                {
                    var voucher = await _voucherRepo.GetByCodeAsync(checkoutDto.PromoCode);
                    if (voucher != null)
                    {
                        voucherId = voucher.VoucherId;
                    }
                    else
                    {
                        return (false, "Mã giảm giá không hợp lệ!", null);
                    }

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
                    // Kiểm tra min order
                    if (voucher.MinOrderAmount.HasValue && totalAmount < voucher.MinOrderAmount.Value)
                    {
                        return (false, $"Đơn hàng phải từ {voucher.MinOrderAmount.Value:N0}₫ mới dùng được mã này", null);
                    }
                }

                // 7. Calculate final amount
                decimal finalAmount = totalAmount + shippingFee - discount;
                finalAmount = Math.Max(finalAmount, 0);

                // 8. Create order
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
                    TotalAmount = finalAmount,
                    PaidByWalletAmount = 0, // Adjust if you have wallet feature
                    PaidByExternalAmount = finalAmount,
                    RefundStatus = "None",
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow
                };

                // 9. Create order details
                foreach (var cartItem in cart?.CartItems ?? new List<CartItem>())
                {
                    var orderDetail = new OrderDetail
                    {
                        Order = order,
                        ProductId = cartItem.ProductId,
                        Quantity = cartItem.Quantity,
                        UnitPrice = cartItem.Product.Price,
                        Discount = 0,
                        Total = cartItem.Quantity * cartItem.Product.Price,
                        IsDeleted = false,
                        Reviewed = false,
                        CreatedAt = DateTime.UtcNow
                    };
                    order.OrderDetails.Add(orderDetail);
                }

                // 10. Save order to database
                int orderId = await _orderRepo.CreateOrderAsync(order);

                // 11. Update product stock
                foreach (var cartItem in cart?.CartItems ?? new List<CartItem>())
                {
                    var product = cartItem.Product;
                    product.Quantity -= cartItem.Quantity;
                    await _productRepo.UpdateAsync(product);
                }

                // 12. Clear cart
                await _cartRepo.ClearCartByAccountIdAsync(accountId);

                return (true, "Đơn hàng được tạo thành công!", orderId);
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi: {ex.Message}", null);
            }
        }
    }
}

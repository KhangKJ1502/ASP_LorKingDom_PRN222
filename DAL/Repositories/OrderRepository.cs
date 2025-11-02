using DAL.Interfaces;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly AspLorKingDomContext _db;

        public OrderRepository(AspLorKingDomContext db)
        {
            _db = db;
        }
        public async Task<int> CreateOrderAsync(Order order)
        {
            _db.Orders.Add(order);
            await _db.SaveChangesAsync();
            return order.OrderId;
        }

        public async Task<int> CountByVoucherAndAccountAsync(int voucherId, int accountId)
        {
            return await _db.Orders
                .CountAsync(o =>
                    o.VoucherId == voucherId &&
                    o.AccountId == accountId && !o.IsDeleted);
        }

        public async Task<List<Order>> GetOrdersByAccountIdAsync(int accountId)
        {
            return await _db.Orders
                .Include(o => o.Status)
                .Include(o => o.Account)
                .Include(o => o.Voucher)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                        .ThenInclude(p => p.ProductImages)
                .Where(o => o.AccountId == accountId && !o.IsDeleted)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<Order?> GetOrderByIdAsync(int orderId)
        {
            return await _db.Orders
                .Include(o => o.Status)
                .Include(o => o.Account)
                .Include(o => o.Voucher)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                        .ThenInclude(p => p.ProductImages)
                .FirstOrDefaultAsync(o => o.OrderId == orderId && !o.IsDeleted);
        }

        public async Task<(List<Order> orders, int totalCount)> GetAllOrdersAsync(string? query, int? statusId, DateTime? dateFrom, DateTime? dateTo, int skip, int take)
        {
            var queryable = _db.Orders
                .Include(o => o.Status)
                .Include(o => o.Account)
                .Include(o => o.Voucher)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                        .ThenInclude(p => p.ProductImages)
                .Where(o => !o.IsDeleted);

            // Search by query (OrderId or ShippingName)
            if (!string.IsNullOrWhiteSpace(query))
            {
                query = query.Trim().ToLower();
                queryable = queryable.Where(o =>
                    o.OrderId.ToString().Contains(query) ||
                    (o.ShippingName != null && o.ShippingName.ToLower().Contains(query)));
            }

            // Filter by status
            if (statusId.HasValue)
            {
                queryable = queryable.Where(o => o.StatusId == statusId.Value);
            }

            // Filter by date range
            if (dateFrom.HasValue)
            {
                queryable = queryable.Where(o => o.OrderDate >= dateFrom.Value);
            }
            if (dateTo.HasValue)
            {
                var dateToEndOfDay = dateTo.Value.Date.AddDays(1).AddTicks(-1);
                queryable = queryable.Where(o => o.OrderDate <= dateToEndOfDay);
            }

            var totalCount = await queryable.CountAsync();

            var orders = await queryable
                .OrderByDescending(o => o.OrderId)
                .Skip(skip)
                .Take(take)
                .ToListAsync();

            return (orders, totalCount);
        }

        public async Task<bool> UpdateOrderStatusAsync(int orderId, int newStatusId)
        {
            var order = await _db.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId && !o.IsDeleted);
            if (order == null)
                return false;

            order.StatusId = newStatusId;
            order.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return true;
        }

		public async Task<List<Order>> GetByAccountIdAsync(int accountId)
		{
			return await _db.Orders
				.Include(o => o.Status)
				.Include(o => o.Account)
				.Include(o => o.Voucher)
				.Include(o => o.OrderDetails)
					.ThenInclude(od => od.Product)
						.ThenInclude(p => p.ProductImages)
				.Where(o => o.AccountId == accountId && !o.IsDeleted)
				.OrderByDescending(o => o.OrderDate)
				.ToListAsync();
		}

		public async Task<List<OrderDetail>> GetOrderDetailsByOrderIdAsync(int orderId)
		{
			return await _db.OrderDetails
				.Include(od => od.Product)
					.ThenInclude(p => p.ProductImages)
				.Where(od => od.OrderId == orderId && !od.IsDeleted)
				.ToListAsync();
		}

		public async Task<bool> UpdateOrderDetailAsync(OrderDetail orderDetail)
		{
			try
			{
				_db.OrderDetails.Update(orderDetail);
				await _db.SaveChangesAsync();
				return true;
			}
			catch
			{
				return false;
			}
		}

	}
}

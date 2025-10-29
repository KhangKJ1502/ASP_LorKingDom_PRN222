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
    }
}

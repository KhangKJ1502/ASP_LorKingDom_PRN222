// DAL/Data/AppDbContext.cs
using Microsoft.EntityFrameworkCore;

namespace DAL.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        // Không cần DbSet ở đây. Khi scaffold DB-First, EF sẽ ghi đè file này (vì bạn dùng --force).
    }
}

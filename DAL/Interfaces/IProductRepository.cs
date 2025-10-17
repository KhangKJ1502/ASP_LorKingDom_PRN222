using DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Interfaces
{
    public interface IProductRepository
    {
        Task<List<(int Id, string Name)>> GetBasicListAsync(string? keyword = null, int limit = 200);
        Task<bool> ExistsAsync(int productId);
    }
}

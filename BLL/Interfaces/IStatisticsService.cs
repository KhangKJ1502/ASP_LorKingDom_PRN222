using BLL.DTOs;
using System.Threading.Tasks;

namespace BLL.Interfaces
{
    public interface IStatisticsService
    {
        Task<DashboardStatisticsDto> GetDashboardStatisticsAsync();
        Task<ProductStatisticsDto> GetProductStatisticsAsync();
    }
}

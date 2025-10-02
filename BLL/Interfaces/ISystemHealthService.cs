// BLL/Interfaces/ISystemHealthService.cs
namespace BLL.Interfaces;

using System.Threading;
using System.Threading.Tasks;
using BLL.DTOs;

public interface ISystemHealthService
{
    Task<DbHealthReport> CheckDatabaseAsync(CancellationToken ct = default);
}

using System.Threading;
using System.Threading.Tasks;

namespace DAL.Interfaces
{
    public interface IDbHealthCheck
    {
        Task<(bool ok, string? message, string? server, string? database, string? version)>
            PingAsync(CancellationToken ct = default);
        
    }
}

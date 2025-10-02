using System.Threading;
using System.Threading.Tasks;
using BLL.DTOs;
using BLL.Interfaces;
using DAL.Interfaces;

namespace BLL.Services
{
    public class SystemHealthService : ISystemHealthService
    {
        private readonly IDbHealthCheck _db;

        public SystemHealthService(IDbHealthCheck db)
        {
            _db = db;
        }

        public async Task<DbHealthReport> CheckDatabaseAsync(CancellationToken ct = default)
        {
            var (ok, message, server, database, version) = await _db.PingAsync(ct);
            return new DbHealthReport
            {
                Ok = ok,
                Message = message,
                Server = server,
                Database = database,
                Version = version
            };
        }
    }
}

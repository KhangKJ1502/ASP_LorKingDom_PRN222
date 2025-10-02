using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using DAL.Data;                    // AppDbContext (sinh từ scaffold)
using DAL.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class DbHealthCheck : IDbHealthCheck
    {
        private readonly AppDbContext _ctx;
        public DbHealthCheck(AppDbContext ctx) => _ctx = ctx;


        public async Task<(bool ok, string? message, string? server, string? database, string? version)>
            PingAsync(CancellationToken ct = default)
        {
            try
            {
                var can = await _ctx.Database.CanConnectAsync(ct);

                string? server = null, db = null, version = null;

                var conn = _ctx.Database.GetDbConnection();
                await conn.OpenAsync(ct);
                await using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT @@SERVERNAME, DB_NAME(), @@VERSION";
                    cmd.CommandType = CommandType.Text;

                    await using var reader = await cmd.ExecuteReaderAsync(ct);
                    if (await reader.ReadAsync(ct))
                    {
                        server = reader.IsDBNull(0) ? null : reader.GetString(0);
                        db = reader.IsDBNull(1) ? null : reader.GetString(1);
                        version = reader.IsDBNull(2) ? null : reader.GetString(2);
                    }
                }
                await conn.CloseAsync();

                return (can, null, server, db, version);
            }
            catch (Exception ex)
            {
                return (false, ex.Message, null, null, null);
            }
        }
    }
}

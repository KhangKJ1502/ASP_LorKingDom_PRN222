using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BLL.DTOs;
using BLL.Interfaces;
using DAL.Interfaces;

namespace BLL.Services
{
    public class SexService : ISexService
    {
        private readonly ISexRepository _repo;

        public SexService(ISexRepository repo)
        {
            _repo = repo;
        }
        public async Task<List<SexDto>> GetActiveAsync()
        {
            var list = await _repo.GetActiveAsync();
            return list.Select(x => new SexDto
            {
                Id = x.SexId,
                Name = x.SexName,
                IsDeleted = x.IsDeleted,
                CreatedAt = x.CreatedAt
            }).ToList();
        }
    }
}

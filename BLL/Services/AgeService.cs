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
    public class AgeService : IAgeService
    {
        private readonly IAgeRepository _repo;

        public AgeService(IAgeRepository repo)
        {
            _repo = repo;
        }

        public async Task<List<AgeDto>> GetActiveAsync()
        {
            var list = await _repo.GetActiveAsync();
            return list.Select(x => new AgeDto
            {
                Id = x.AgeId,
                AgeRange = x.AgeRange,
                IsDeleted = x.IsDeleted,
                CreatedAt = x.CreatedAt
            }).ToList();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BLL.DTOs;

namespace BLL.Interfaces
{
    public interface ISexService
    {
        Task<List<SexDto>> GetActiveAsync();
    }
}

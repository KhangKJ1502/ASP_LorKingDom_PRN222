using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Interfaces
{
    public interface IAvatarService
    {
        Task<(bool Success, string MessageOrPath)> SaveAvatarAsync(IFormFile file);
        void DeleteAvatar(string? imagePath);
        (bool IsValid, string? ErrorMessage) ValidateAvatarFile(IFormFile file);
    }

}

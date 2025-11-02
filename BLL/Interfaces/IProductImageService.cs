using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Interfaces
{
    public interface IProductImageService
    {
        Task<int> AddImagesAsync(int productId, string mainImageUrl, IEnumerable<string> secondaryImageUrls);
        Task UpsertImagesAsync(
     int productId,
     string? mainImageUrl,
     IEnumerable<string> keepSecondaryUrls,
     IEnumerable<string> addSecondaryUrls,
     bool keepMainIfNull = true);
    }
}

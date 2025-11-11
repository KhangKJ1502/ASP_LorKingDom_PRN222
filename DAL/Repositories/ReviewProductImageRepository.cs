using DAL.Interfaces;
using DAL.Models;
using System;
using System.Threading.Tasks;

namespace DAL.Repositories
{
    public class ReviewProductImageRepository : IReviewProductImageRepository
    {
        private readonly AspLorKingDomContext _context;

        public ReviewProductImageRepository(AspLorKingDomContext context)
        {
            _context = context;
        }

        public async Task<int> CreateAsync(ReviewProductImage image)
        {
            await _context.ReviewProductImages.AddAsync(image);
            await _context.SaveChangesAsync();
            return image.ReviewProductImageId;
        }

        public async Task<bool> DeleteAsync(int imageId)
        {
            var image = await _context.ReviewProductImages.FindAsync(imageId);
            if (image == null) return false;

            image.IsDeleted = true;
            image.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
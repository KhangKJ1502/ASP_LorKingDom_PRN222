using BLL.DTOs;
using BLL.Interfaces;
using BLL.Validators;
using DAL.Interfaces;
using DAL.Models;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _repo;
    private readonly ISuperCategoryRepository _superRepo; // nếu cần validate
    private readonly IProductRepository _productRepo;
    public CategoryService(ICategoryRepository repo, ISuperCategoryRepository superRepo, IProductRepository productRepo)
    {
        _repo = repo;
        _superRepo = superRepo;
        _productRepo = productRepo;
    }

    private static CategoryDto Map(Category x) => new()
    {
        Id = x.CategoryId,
        SuperCategoryId = x.SuperCategoryId,
        Name = x.CategoryName,
        IsDeleted = x.IsDeleted,
        CreatedAt = x.CreatedAt,
        SuperCategoryName = x.SuperCategory?.SuperCategoryName
    };

    public async Task<List<CategoryDto>> GetAllAsync(string? keyword = null)
        => (await _repo.GetAllAsync(keyword)).Select(Map).ToList();

    public async Task<List<CategoryDto>> GetActiveAsync()
        => (await _repo.GetActiveAsync()).Select(Map).ToList();

    public async Task<CategoryDto?> GetByIdAsync(int id)
    {
        var e = await _repo.GetByIdAsync(id);
        return e == null ? null : Map(e);
    }

    public async Task<int> CreateAsync(int superCategoryId, string name, bool isDeleted = false)
    {
        var super = await _superRepo.GetByIdAsync(superCategoryId)
            ?? throw new InvalidOperationException("Nhóm danh mục tổng không tồn tại.");
        if (!isDeleted && super.IsDeleted)
            throw new InvalidOperationException("Không thể tạo danh mục hoạt động vì Nhóm danh mục tổng đang không hoạt động.");
        var dto = new CategoryDto
        {
            SuperCategoryId = superCategoryId,
            Name = (name ?? string.Empty).Trim(),
            IsDeleted = isDeleted
        };
        CategoryValidator.Validate(dto);
        // optional: validate superCategoryId tồn tại
        var entity = new Category
        {
            SuperCategoryId = superCategoryId,
            CategoryName = name.Trim(),
            IsDeleted = isDeleted,
            CreatedAt = DateTime.UtcNow
        };

        if (await _repo.ExistsByNameAsync(entity.CategoryName))
            throw new InvalidOperationException("Tên danh mục đã tồn tại.");

        await _repo.AddAsync(entity);
        return entity.CategoryId;
    }

    public async Task<bool> UpdateAsync(int id, int superCategoryId, string name, bool isDeleted)
    {
        var super = await _superRepo.GetByIdAsync(superCategoryId)
            ?? throw new InvalidOperationException("Nhóm danh mục tổng không tồn tại.");
        if (!isDeleted && super.IsDeleted)
            throw new InvalidOperationException("Không thể đặt hoạt động vì Nhóm danh mục tổng đang không hoạt động.");

        var e = await _repo.GetByIdAsync(id);
        if (e == null) return false;

        var newName = name.Trim();
        if (await _repo.ExistsByNameAsync(newName, id))
            throw new InvalidOperationException("Tên danh mục đã tồn tại.");

        e.SuperCategoryId = superCategoryId;
        e.CategoryName = newName;
        e.IsDeleted = isDeleted;

        await _repo.UpdateAsync(e);
        if (isDeleted)
        {
            await _productRepo.SetIsDeletedByCategoryAsync(id, true);
            // Quy ước: bật lại Category không tự bật Product (giữ như trước)
        }
        return true;
    }

    // ✅ Phân trang
    public async Task<PagedResult<CategoryDto>> GetPagedAsync(string? keyword, int page, int pageSize)
    {
        var (items, total) = await _repo.QueryPagedAsync(keyword, page, pageSize);
        return new PagedResult<CategoryDto>
        {
            Items = items.Select(Map).ToList(),
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }
}



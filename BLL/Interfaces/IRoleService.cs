namespace BLL.Interfaces
{
    public interface IRoleService
    {
        Task<string?> GetRoleNameByIdAsync(int? roleId);
    }
}

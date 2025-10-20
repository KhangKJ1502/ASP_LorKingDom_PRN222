using BLL.Interfaces;
using DAL.Interfaces;

namespace BLL.Services
{
    public class RoleService : IRoleService
    {
        private readonly IRoleRepository _roleRepo;

        public RoleService(IRoleRepository roleRepo)
        {
            _roleRepo = roleRepo;
        }

        public async Task<string?> GetRoleNameByIdAsync(int? roleId)
        {
            if (!roleId.HasValue) return null;
            var role = await _roleRepo.GetByIdAsync(roleId.Value);
            return role?.RoleName;
        }
    }
}

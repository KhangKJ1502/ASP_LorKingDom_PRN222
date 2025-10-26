using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace WebUI.Filters
{
    /// <summary>
    /// Custom Authorization Attribute cho Admin
    /// Chỉ Admin mới có quyền truy cập
    /// </summary>
    public class AdminOnlyAttribute : AuthorizeAttribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;

            if (!user.Identity?.IsAuthenticated ?? true)
            {
                context.Result = new RedirectToActionResult("Login", "AdminAuth", null);
                return;
            }

            var role = user.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            if (role != "Admin")
            {
                context.Result = new RedirectToActionResult("AccessDenied", "AdminAuth", null);
            }
        }
    }

    /// <summary>
    /// Custom Authorization Attribute cho Admin và Staff
    /// Warehouse không có quyền
    /// </summary>
    public class AdminAndStaffOnlyAttribute : AuthorizeAttribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;

            if (!user.Identity?.IsAuthenticated ?? true)
            {
                context.Result = new RedirectToActionResult("Login", "AdminAuth", null);
                return;
            }

            var role = user.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            if (role != "Admin" && role != "Staff")
            {
                context.Result = new RedirectToActionResult("AccessDenied", "AdminAuth", null);
            }
        }
    }

    /// <summary>
    /// Custom Authorization Attribute cho tất cả role quản lý
    /// Admin, Staff, Warehouse đều có quyền
    /// </summary>
    public class ManagementOnlyAttribute : AuthorizeAttribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;

            if (!user.Identity?.IsAuthenticated ?? true)
            {
                context.Result = new RedirectToActionResult("Login", "AdminAuth", null);
                return;
            }

            var role = user.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            if (role != "Admin" && role != "Staff" && role != "Warehouse")
            {
                context.Result = new RedirectToActionResult("AccessDenied", "AdminAuth", null);
            }
        }
    }
}

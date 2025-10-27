using BLL.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace WebUI.Controllers;

[Route("addresses")]
[AutoValidateAntiforgeryToken]
public class AddressController : Controller
{
    private readonly IAddressService _service;
    public AddressController(IAddressService service) => _service = service;

    int GetCurrentAccountId()
        => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    // Tab content (partial) – chứa grid + modals
    [HttpGet("partial")]
    [IgnoreAntiforgeryToken]
    public IActionResult Partial() => PartialView("~/Views/Home/_ProfileAddressesPartial.cshtml");

    // Danh sách JSON (AJAX)
    [HttpGet("list")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> List()
    {
        var accountId = GetCurrentAccountId();
        if (accountId == 0) return Unauthorized();
        var list = await _service.GetByAccountIdAsync(accountId);
        return Json(list);
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create(string addressLine, string city, string? ward, bool? isDefault)
    {
        var accountId = GetCurrentAccountId();
        if (accountId == 0) return Unauthorized();
        var id = await _service.AddAsync(accountId, addressLine, city, ward, isDefault);
        return Json(new { ok = true, id });
    }

    [HttpPost("update")]
    public async Task<IActionResult> Update(int id, string addressLine, string city, string? ward, bool? isDefault)
    {
        var accountId = GetCurrentAccountId();
        if (accountId == 0) return Unauthorized();
        await _service.UpdateAsync(id, accountId, addressLine, city, ward, isDefault);
        return Json(new { ok = true });
    }

    [HttpPost("delete")]
    public async Task<IActionResult> Delete(int id)
    {
        var accountId = GetCurrentAccountId();
        if (accountId == 0) return Unauthorized();
        await _service.DeleteAsync(id, accountId);
        return Json(new { ok = true });
    }

    [HttpPost("set-default")]
    public async Task<IActionResult> SetDefault(int id)
    {
        var accountId = GetCurrentAccountId();
        if (accountId == 0) return Unauthorized();
        await _service.SetDefaultAsync(id, accountId);
        return Json(new { ok = true });
    }
}

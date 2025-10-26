using BLL.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebUI.Controllers
{
    [Route("addresses")]
    [AutoValidateAntiforgeryToken]
    public class AddressController : Controller
    {
        private readonly IAddressService _service;

        public AddressController(IAddressService service)
        {
            _service = service;
        }

        [HttpGet("partial")]
        public IActionResult Partial()
        {
            return PartialView("~/Views/Home/_ProfileAddressesPartial.cshtml");
        }

        [HttpGet("list")]
        public async Task<IActionResult> List()
        {
            int accountId = GetCurrentAccountId();
            var list = await _service.GetByAccountIdAsync(accountId);
            return Json(list);
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create(string addressLine, string city, string? ward, bool isDefault)
        {
            int accountId = GetCurrentAccountId();
            await _service.AddAsync(accountId, addressLine, city, ward, isDefault);
            return Ok(new { ok = true });
        }

        [HttpPost("update")]
        public async Task<IActionResult> Update(int id, string addressLine, string city, string? ward, bool isDefault)
        {
            await _service.UpdateAsync(id, addressLine, city, ward, isDefault);
            return Ok(new { ok = true });
        }

        [HttpPost("delete")]
        public async Task<IActionResult> Delete(int id)
        {
            await _service.DeleteAsync(id);
            return Ok(new { ok = true });
        }

        private int GetCurrentAccountId() => 1; // TODO: lấy từ Claims/Identity
    }
}

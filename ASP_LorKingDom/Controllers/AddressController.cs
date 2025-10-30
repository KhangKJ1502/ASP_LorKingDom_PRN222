using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BLL.Interfaces;

namespace WebUI.Controllers
{
    [Authorize]
    public class AddressController : Controller
    {
        private readonly IAddressService _addrSvc;

        public AddressController(IAddressService addrSvc)
        {
            _addrSvc = addrSvc;
        }

        private bool TryGetAccountId(out int accountId)
        {
            accountId = 0;
            var s = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(s, out accountId);
        }

        private static readonly List<string> CityList = new()
        {
            "Hà Nội", "TP Hồ Chí Minh", "Đà Nẵng", "Hải Phòng", "Cần Thơ",
            "Bắc Giang", "Bắc Ninh", "Quảng Ninh", "Hải Dương", "Hưng Yên",
            "Thanh Hóa", "Nghệ An", "Hà Tĩnh", "Thừa Thiên Huế", "Đắk Lắk",
            "Khánh Hòa", "Bình Dương", "Đồng Nai", "Bà Rịa - Vũng Tàu", "Cà Mau"
        };

        // ===== CRUD chuẩn (non-AJAX) =====

        // ===== AJAX partials =====

        [HttpGet]
        public async Task<IActionResult> ListPartial(string? q = null)
        {
            if (!TryGetAccountId(out var accountId)) return Challenge();

            var list = await _addrSvc.ListAsync(accountId, q);
            ViewBag.Query = q;
            return PartialView("~/Views/Home/_AddressListPartial.cshtml", list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAjax(string city, string ward, string addressLine, bool setAsDefault = false, string? q = null)
        {
            if (!TryGetAccountId(out var accountId)) return Challenge();

            try
            {
                await _addrSvc.CreateAsync(accountId, city, ward, addressLine, setAsDefault);
                var list = await _addrSvc.ListAsync(accountId, q);
                ViewBag.Query = q;
                return PartialView("~/Views/Home/_AddressListPartial.cshtml", list);
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
            catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
            catch (Exception) { return StatusCode(500, "Đã xảy ra lỗi không mong muốn. Vui lòng thử lại."); }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAjax(int id, string city, string ward, string addressLine, bool setAsDefault = false, string? q = null)
        {
            if (!TryGetAccountId(out var accountId)) return Challenge();

            try
            {
                var ok = await _addrSvc.UpdateAsync(accountId, id, city, ward, addressLine, setAsDefault);
                if (!ok) return NotFound("Không tìm thấy địa chỉ để cập nhật.");
                var list = await _addrSvc.ListAsync(accountId, q);
                ViewBag.Query = q;
                return PartialView("~/Views/Home/_AddressListPartial.cshtml", list);
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
            catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
            catch (Exception) { return StatusCode(500, "Đã xảy ra lỗi không mong muốn. Vui lòng thử lại."); }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAjax(int id, string? q = null)
        {
            if (!TryGetAccountId(out var accountId)) return Challenge();

            try
            {
                var ok = await _addrSvc.DeleteAsync(accountId, id);
                if (!ok) return NotFound("Không tìm thấy địa chỉ để xóa.");
                var list = await _addrSvc.ListAsync(accountId, q);
                ViewBag.Query = q;
                return PartialView("~/Views/Home/_AddressListPartial.cshtml", list);
            }
            catch (Exception) { return StatusCode(500, "Không thể xóa địa chỉ lúc này."); }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetDefaultAjax(int id, string? q = null)
        {
            if (!TryGetAccountId(out var accountId)) return Challenge();

            try
            {
                var changed = await _addrSvc.SetDefaultAsync(accountId, id);
                if (!changed) return BadRequest("Địa chỉ đã là mặc định.");
                var list = await _addrSvc.ListAsync(accountId, q);
                ViewBag.Query = q;
                return PartialView("~/Views/Home/_AddressListPartial.cshtml", list);
            }
            catch (Exception) { return StatusCode(500, "Không thể đặt mặc định lúc này."); }
        }


        // ===== Form partials (Add / Edit) =====

        [HttpGet]
        public IActionResult FormPartialCreate()
        {
            ViewBag.Cities = CityList;
            return PartialView("~/Views/Home/_AddressFormPartial.cshtml", model: null);
        }

        [HttpGet]
        public async Task<IActionResult> FormPartialEdit(int id)
        {
            if (!TryGetAccountId(out var accountId)) return Challenge();

            var dto = await _addrSvc.GetAsync(accountId, id);
            if (dto == null) return NotFound();

            ViewBag.Cities = CityList;
            return PartialView("~/Views/Home/_AddressFormPartial.cshtml", dto);
        }
    }
}

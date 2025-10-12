using Microsoft.AspNetCore.Mvc;
using BLL.Interfaces;

namespace WebUI.Controllers
{
    public class SuperCategoryController : Controller
    {
        private readonly ISuperCategoryService _service;

        public SuperCategoryController(ISuperCategoryService service)
        {
            _service = service;
        }

        // GET: /SuperCategory/Manage
        public async Task<IActionResult> Manage(string? q)
        {
            var list = await _service.GetAllAsync(q);
            ViewBag.Query = q;

            // View của bạn nằm trong thư mục Admin
            return View("~/Views/Admin/ManageSuperCategory.cshtml", list);
        }

        // POST: /SuperCategory/SaveSuperCategory
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveSuperCategory(int id, string name, bool isDeleted = false)
        {
            try
            {
                if (id == 0)
                    await _service.CreateAsync(name, isDeleted);
                else
                    await _service.UpdateAsync(id, name, isDeleted);

                TempData["Success"] = "Lưu thành công!";
                return RedirectToAction(nameof(Manage)); // PRG khi thành công
            }
            catch (InvalidOperationException ex) // lỗi do validator/dup name...
            {
                // Load lại dữ liệu list
                var list = await _service.GetAllAsync();
                ViewBag.Query = null;

                ModelState.AddModelError("name", ex.Message); // hiện "Tên đã tồn tại." cạnh input

                if (id == 0) ViewBag.LastName = name; // giữ lại tên vừa nhập ở form thêm mới


                // 1) Bật modal lỗi
                ViewBag.ErrorMessage = ex.Message;          // nội dung lỗi
                ViewBag.ShowErrorModal = true;              // trigger hiển thị

                // 2) Nếu là update, mở lại modal Edit với dữ liệu đang sửa
                if (id != 0)
                {
                    // Lấy entity hiện có; nếu null thì dùng giá trị user vừa submit
                    var dto = await _service.GetByIdAsync(id)
                              ?? new BLL.DTOs.SuperCategoryDto
                              {
                                  Id = id,
                                  Name = name,
                                  IsDeleted = isDeleted
                              };

                    ViewBag.EditSuperCategory = dto;
                }

                // Trả về lại cùng View, không redirect => vẫn "cùng một trang"
                return View("~/Views/Admin/ManageSuperCategory.cshtml", list);
            }
            catch (Exception ex)
            {
                var list = await _service.GetAllAsync();
                ViewBag.ErrorMessage = "Đã có lỗi xảy ra. " + ex.Message;
                ViewBag.ShowErrorModal = true;
                return View("~/Views/Admin/ManageSuperCategory.cshtml", list);
            }
        }


        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var dto = await _service.GetByIdAsync(id);
            if (dto == null)
            {
                TempData["Error"] = "Không tìm thấy danh mục.";
                return RedirectToAction(nameof(Manage));
            }

            var list = await _service.GetAllAsync();
            ViewBag.EditSuperCategory = dto;  // truyền vào ViewBag để modal hiển thị
            return View("~/Views/Admin/ManageSuperCategory.cshtml", list);
        }

    }
}

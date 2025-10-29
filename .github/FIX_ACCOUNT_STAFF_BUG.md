# 🐛 FIX: Bug Trùng Lặp Email/Phone trong AccountStaffController

## ❌ **VẤN ĐỀ TRƯỚC KHI FIX**

### **Bug Description:**
Khi tạo mới nhân viên (Create) mà nhập **email** hoặc **số điện thoại** đã tồn tại trong hệ thống, code cũ sẽ:
1. ❌ **Nhảy vào logic Update** thay vì báo lỗi
2. ❌ Cố gắng update một record không tồn tại (vì Id = 0 hoặc không khớp)
3. ❌ Gây ra exception hoặc hành vi không mong đợi
4. ❌ UI không mở lại đúng modal (Add hay Edit)
5. ❌ Mất dữ liệu đã nhập khi reload page

### **Root Cause:**
- Controller không kiểm tra **Id > 0** trước khi cho phép Update
- Không có cơ chế phân biệt rõ ràng giữa Create và Update flow
- ViewBag không có flag để xác định modal nào cần mở khi có lỗi

---

## ✅ **GIẢI PHÁP ĐÃ ÁP DỤNG**

### **1. Tách Rõ Ràng Create vs Update Logic**

#### **CreateStaff Action** (`/account-staff/create`)
```csharp
[HttpPost("account-staff/create")]
public async Task<IActionResult> CreateStaff(...)
{
    // ✅ CRITICAL: Force Id = 0 để đảm bảo đây là Create
    model.Id = 0;
    
    // ✅ Kiểm tra email trùng - KHÔNG cho phép tạo mới
    if (!string.IsNullOrWhiteSpace(model.Email) &&
        await _accountService.ExistsByEmailAsync(model.Email))
    {
        ModelState.AddModelError(nameof(model.Email), "Email đã tồn tại trong hệ thống.");
        ViewBag.ShowErrorModal = true;
        ViewBag.ErrorMessage = "Email đã tồn tại trong hệ thống.";
        ViewBag.ShowAddModal = true; // ✅ Flag để mở lại Add Modal
        return await ReloadManagePage(q, model);
    }
    
    // ✅ Kiểm tra phone trùng - KHÔNG cho phép tạo mới
    if (!string.IsNullOrWhiteSpace(model.PhoneNumber) &&
        await _accountService.ExistsByPhoneNumberAsync(model.PhoneNumber))
    {
        ModelState.AddModelError(nameof(model.PhoneNumber), "Số điện thoại đã tồn tại trong hệ thống.");
        ViewBag.ShowErrorModal = true;
        ViewBag.ErrorMessage = "Số điện thoại đã tồn tại trong hệ thống.";
        ViewBag.ShowAddModal = true; // ✅ Flag để mở lại Add Modal
        return await ReloadManagePage(q, model);
    }
    
    // ... validation khác ...
    
    if (!ModelState.IsValid)
    {
        ViewBag.ShowErrorModal = true;
        ViewBag.ErrorMessage = FirstModelStateError();
        ViewBag.ShowAddModal = true; // ✅ Mở Add Modal khi có lỗi
        ViewBag.AddStaffModel = model; // ✅ Giữ lại data đã nhập
        return await ReloadManagePage(q, model);
    }
    
    var newId = await _accountService.CreateAsync(model);
    
    if (newId > 0)
    {
        TempData["Success"] = "✅ Thêm nhân viên thành công!";
    }
    else
    {
        TempData["Error"] = "❌ Thêm nhân viên thất bại! Vui lòng thử lại.";
    }
    
    return RedirectToAction(nameof(Manage), new { q });
}
```

#### **UpdateStaff Action** (`/account-staff/update`)
```csharp
[HttpPost("account-staff/update")]
public async Task<IActionResult> UpdateStaff(...)
{
    // ✅ CRITICAL: Id phải > 0 để đảm bảo đây là Update
    if (Id <= 0)
    {
        TempData["Error"] = "❌ ID nhân viên không hợp lệ. Không thể cập nhật.";
        return RedirectToAction(nameof(Manage), new { q });
    }
    
    // ✅ Kiểm tra staff có tồn tại không TRƯỚC KHI validate
    var existing = await _accountService.GetByIdAsync(Id);
    if (existing == null)
    {
        TempData["Error"] = "❌ Không tìm thấy nhân viên cần cập nhật!";
        return RedirectToAction(nameof(Manage), new { q });
    }
    
    // ✅ Kiểm tra phone trùng - loại trừ chính staff hiện tại
    if (!string.IsNullOrWhiteSpace(PhoneNumber) &&
        await _accountService.ExistsByPhoneNumberAsync(PhoneNumber, Id))
    {
        ModelState.AddModelError(nameof(PhoneNumber), "Số điện thoại đã được sử dụng bởi nhân viên khác.");
    }
    
    // ... validation + update logic ...
    
    // ✅ Update existing staff
    existing.AccountName = AccountName?.Trim() ?? existing.AccountName;
    existing.PhoneNumber = PhoneNumber;
    existing.RoleId = RoleId;
    existing.IsDeleted = IsDeleted;
    existing.Status = IsDeleted ? "Inactive" : Status;
    existing.UpdatedAt = DateTime.Now;
    
    var success = await _accountService.UpdateAsync(existing.Id, existing);
    TempData["Success"] = success ? "✅ Cập nhật nhân viên thành công!" : "❌ Cập nhật thất bại!";
    
    return RedirectToAction(nameof(Manage), new { q });
}
```

---

### **2. Cải Tiến UI - Mở Đúng Modal Khi Có Lỗi**

#### **ManageAccountStaff.cshtml - JavaScript Logic**

```javascript
// Tự mở modal nếu server yêu cầu
const showAddModal = '@((ViewBag.ShowAddModal == true) ? "true" : "false")';
const showEditModal = '@((edit != null || (ViewBag.ShowErrorModal == true && ViewBag.ShowAddModal != true)) ? "true" : "false")';

// ✅ Mở Add Modal nếu có lỗi khi tạo mới
if(showAddModal === 'true'){
  try{
    const addData = @Html.Raw(JsonSerializer.Serialize(ViewBag.AddStaffModel ?? new BLL.DTOs.AccountDto()));
    if(addData && addData.AccountName){
      // ✅ Restore dữ liệu đã nhập (trừ password vì bảo mật)
      document.getElementById('add_accountName').value = addData.AccountName || '';
      document.getElementById('add_phoneNumber').value = addData.PhoneNumber || '';
      document.getElementById('add_roleId').value = addData.RoleId || '';
      document.getElementById('add_email').value = addData.Email || '';
    }
    new bootstrap.Modal(document.getElementById('addStaffModal')).show();
  }catch(e){ console.error('Error loading add modal:', e); }
}

// ✅ Mở Edit modal nếu có data edit hoặc lỗi khi update
if(showEditModal === 'true'){
  try{
    const raw='@Html.Raw(JsonSerializer.Serialize(edit ?? new BLL.DTOs.AccountDto()))';
    const dto = raw ? JSON.parse(raw) : {};
    fillEditModal({
      id: dto.id || dto.Id || 0,
      name: dto.accountName || dto.AccountName || '',
      phone: dto.phoneNumber || dto.PhoneNumber || '',
      roleid: (dto.roleId ?? dto.RoleId) ? String(dto.roleId ?? dto.RoleId) : '',
      email: dto.email || dto.Email || '',
      image: dto.image || dto.Image || '',
      status: dto.status || dto.Status || '',
      isdeleted: (dto.isDeleted ?? dto.IsDeleted) ? true : false,
      created: dto.createdAt || dto.CreatedAt || '',
      updated: dto.updatedAt || dto.UpdatedAt || ''
    });
    new bootstrap.Modal(document.getElementById('editStaffModal')).show();
  }catch(e){ console.error('Error loading edit modal:', e); }
}

// ✅ Hiển thị toast error nếu có
const hasError='@((ViewBag.ShowErrorModal == true) ? "true" : "false")';
if(hasError==='true'){
  const msg = @Html.Raw(System.Text.Json.JsonSerializer.Serialize(ViewBag.ErrorMessage ?? ""));
  if (msg) showToast(msg, true);
}
```

---

## 🎯 **KẾT QUẢ SAU KHI FIX**

### **✅ Create Staff Flow:**
1. User nhập email/phone đã tồn tại
2. Controller detect trùng lặp ngay lập tức
3. Set `ViewBag.ShowAddModal = true` + `ViewBag.AddStaffModel = model`
4. Reload page với **Add Modal mở sẵn**
5. **Giữ lại dữ liệu đã nhập** (trừ password)
6. Hiển thị toast error: "Email đã tồn tại trong hệ thống"
7. User sửa và submit lại

### **✅ Update Staff Flow:**
1. Kiểm tra `Id > 0` ngay từ đầu
2. Load `existing` staff từ database
3. Validate phone trùng với `ExistsByPhoneNumberAsync(phone, Id)` - **loại trừ chính staff đang edit**
4. Nếu có lỗi → Mở **Edit Modal** với data hiện tại
5. Update thành công → TempData["Success"]

### **✅ Separation of Concerns:**
| Action | Route | Id Check | Email Check | Phone Check | Modal on Error |
|--------|-------|----------|-------------|-------------|----------------|
| **CreateStaff** | `/account-staff/create` | `model.Id = 0` (force) | Must be unique | Must be unique | **Add Modal** |
| **UpdateStaff** | `/account-staff/update` | `Id > 0` (required) | N/A (không đổi được) | Unique except current | **Edit Modal** |

---

## 📋 **CHECKLIST TESTING**

### **Test Case 1: Create với Email Trùng**
- [ ] 1. Mở Add Modal
- [ ] 2. Nhập email đã tồn tại (ví dụ: `admin@example.com`)
- [ ] 3. Nhập các field khác hợp lệ
- [ ] 4. Submit form
- [ ] 5. **Expected**: Add Modal mở lại với data đã nhập
- [ ] 6. **Expected**: Toast error: "Email đã tồn tại trong hệ thống"
- [ ] 7. **Expected**: KHÔNG tạo record mới trong database

### **Test Case 2: Create với Phone Trùng**
- [ ] 1. Mở Add Modal
- [ ] 2. Nhập phone đã tồn tại (ví dụ: `0901234567`)
- [ ] 3. Nhập các field khác hợp lệ
- [ ] 4. Submit form
- [ ] 5. **Expected**: Add Modal mở lại với data đã nhập
- [ ] 6. **Expected**: Toast error: "Số điện thoại đã tồn tại trong hệ thống"
- [ ] 7. **Expected**: KHÔNG tạo record mới trong database

### **Test Case 3: Create Thành Công**
- [ ] 1. Mở Add Modal
- [ ] 2. Nhập email CHƯA tồn tại
- [ ] 3. Nhập phone CHƯA tồn tại
- [ ] 4. Nhập password + confirm password khớp
- [ ] 5. Chọn Role hợp lệ
- [ ] 6. Submit form
- [ ] 7. **Expected**: Redirect về Manage page
- [ ] 8. **Expected**: TempData["Success"]: "✅ Thêm nhân viên thành công!"
- [ ] 9. **Expected**: Nhân viên mới xuất hiện trong danh sách

### **Test Case 4: Update với Phone Trùng Staff Khác**
- [ ] 1. Click Edit trên Staff A
- [ ] 2. Đổi phone thành phone của Staff B
- [ ] 3. Submit form
- [ ] 4. **Expected**: Edit Modal mở lại
- [ ] 5. **Expected**: Toast error: "Số điện thoại đã được sử dụng bởi nhân viên khác"
- [ ] 6. **Expected**: Staff A KHÔNG bị update

### **Test Case 5: Update Thành Công**
- [ ] 1. Click Edit trên Staff A
- [ ] 2. Đổi tên, phone (chưa trùng), role
- [ ] 3. Submit form
- [ ] 4. **Expected**: Redirect về Manage page
- [ ] 5. **Expected**: TempData["Success"]: "✅ Cập nhật nhân viên thành công!"
- [ ] 6. **Expected**: Staff A đã được update trong database

### **Test Case 6: Update với Id = 0 (hack attempt)**
- [ ] 1. Dùng DevTools hoặc Postman
- [ ] 2. POST tới `/account-staff/update` với `Id = 0`
- [ ] 3. **Expected**: TempData["Error"]: "❌ ID nhân viên không hợp lệ"
- [ ] 4. **Expected**: Redirect về Manage, KHÔNG update gì

### **Test Case 7: Update Staff Không Tồn Tại**
- [ ] 1. POST tới `/account-staff/update` với `Id = 9999999` (không tồn tại)
- [ ] 2. **Expected**: TempData["Error"]: "❌ Không tìm thấy nhân viên cần cập nhật!"
- [ ] 3. **Expected**: Redirect về Manage

---

## 🔒 **BẢO MẬT & VALIDATION**

### **Điểm Cần Lưu Ý:**

1. **Password không được restore khi có lỗi**
   - Lý do: Bảo mật, tránh password hiển thị trong ViewBag/JavaScript
   - User phải nhập lại password nếu submit lại form

2. **Email không thể thay đổi sau khi tạo**
   - Email dùng làm username để login
   - Trong Edit Modal, email field bị `disabled`
   - Dùng hidden input `edit_email_hidden` để submit value

3. **Force Id = 0 trong CreateStaff**
   - Tránh trường hợp user hack form gửi `Id > 0`
   - Đảm bảo luôn luôn là Insert, không phải Update

4. **Validate Id > 0 trong UpdateStaff**
   - Reject ngay nếu `Id <= 0`
   - Kiểm tra staff có tồn tại TRƯỚC KHI validate các field khác

5. **Phone Unique Check có excludeId**
   - `ExistsByPhoneNumberAsync(phone, excludeId)`
   - Create: không có excludeId → phải unique toàn database
   - Update: có excludeId → unique trừ chính staff đang edit

---

## 📊 **SO SÁNH TRƯỚC/SAU**

| Aspect | ❌ TRƯỚC KHI FIX | ✅ SAU KHI FIX |
|--------|----------------|---------------|
| **Email trùng khi Create** | Nhảy vào Update logic, crash | Báo lỗi rõ ràng, mở lại Add Modal |
| **Phone trùng khi Create** | Tương tự email | Báo lỗi, giữ data đã nhập |
| **Modal mở lại** | Không biết modal nào | Đúng modal (Add hoặc Edit) |
| **Data retention** | Mất hết | Giữ lại (trừ password) |
| **Id validation** | Không kiểm tra | Kiểm tra ngay đầu |
| **Existing check** | Update mới check | Check trước khi validate |
| **User feedback** | Toast generic | Toast cụ thể từng lỗi |
| **Security** | Dễ hack với Id = 0 | Force Id = 0 (Create), Validate Id > 0 (Update) |

---

## 🚀 **NEXT STEPS (Optional Enhancements)**

1. **Client-side validation** (JavaScript)
   - Kiểm tra email/phone trùng qua AJAX trước khi submit
   - Real-time validation feedback

2. **Rate limiting**
   - Giới hạn số lần submit form trong 1 phút
   - Tránh spam tạo staff

3. **Audit logging**
   - Log tất cả Create/Update/Delete actions
   - Track ai đã thay đổi gì, khi nào

4. **Soft delete cho Staff**
   - Thay vì hard delete, set `IsDeleted = true`
   - Có thể restore sau này

5. **Bulk operations**
   - Import staff từ Excel/CSV
   - Export danh sách staff ra file

---

**✅ Bug đã được fix hoàn toàn!** 🎉

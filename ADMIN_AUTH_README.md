# 🔐 Hệ thống đăng nhập Admin/Staff/Warehouse

## ✅ Đã hoàn thành

### 1. **Authentication Scheme riêng biệt**

-   **CustomerScheme**: Dành cho khách hàng (cookie: `.AspNetCore.Cookies`)
-   **AdminScheme**: Dành cho Admin/Staff/Warehouse (cookie: `AdminAuth`)
-   ✅ Cho phép đăng nhập đồng thời cả 2 role trên cùng trình duyệt

### 2. **Trang đăng nhập Admin**

-   **URL**: `/AdminAuth/Login`
-   **Giao diện**: Tối giản, chuyên nghiệp (Bootstrap 5)
-   **Tính năng**:
    -   ✅ Đăng nhập bằng Email + Password (đã hash)
    -   ✅ Ghi nhớ đăng nhập
    -   ✅ Quên mật khẩu (link sẵn sàng)
    -   ✅ Thông báo SweetAlert2
    -   ✅ Kiểm tra role (chỉ Admin/Staff/Warehouse)

### 3. **Phân quyền**

Đã tạo 3 custom attributes:

#### `[AdminOnly]`

-   Chỉ **Admin** mới truy cập được
-   Ví dụ: Quản lý tài khoản Staff, Warehouse

#### `[AdminAndStaffOnly]`

-   **Admin** và **Staff** truy cập được
-   **Warehouse** không có quyền
-   Ví dụ: Quản lý Blog, Blog Category

#### `[ManagementOnly]`

-   **Admin**, **Staff**, **Warehouse** đều truy cập được
-   Ví dụ: Dashboard, xem báo cáo

### 4. **\_AdminLayout.cshtml đã cập nhật**

-   ✅ Avatar động từ Claims
-   ✅ Username hiển thị ở sidebar
-   ✅ Role hiển thị (Admin/Staff/Warehouse)
-   ✅ Profile Modal (xem thông tin nhanh)
-   ✅ Account Settings → `/Admin/Profile`
-   ✅ Logout với xác nhận SweetAlert2

---

## 📖 Cách sử dụng

### **1. Đăng nhập Admin**

```
URL: https://localhost:5001/AdminAuth/Login
Email: admin@example.com
Password: (mật khẩu đã có trong DB)
```

### **2. Áp dụng phân quyền cho Controller**

#### Toàn bộ Controller (tất cả action)

```csharp
using Microsoft.AspNetCore.Authorization;
using WebUI.Filters;

[Authorize(AuthenticationSchemes = "AdminScheme")]
[AdminOnly] // Hoặc [AdminAndStaffOnly], [ManagementOnly]
public class AccountStaffController : Controller
{
    // Tất cả action yêu cầu Admin
}
```

#### Từng Action riêng lẻ

```csharp
[Authorize(AuthenticationSchemes = "AdminScheme")]
public class ProductController : Controller
{
    [ManagementOnly] // Tất cả role xem được
    public IActionResult Index() { }

    [AdminOnly] // Chỉ Admin mới xóa được
    public IActionResult Delete(int id) { }
}
```

### **3. Lấy thông tin user trong View**

```csharp
// Tên tài khoản
@User.FindFirst("AccountName")?.Value

// Email
@User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value

// Role
@User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value

// Avatar
@User.FindFirst("Avatar")?.Value

// User ID
@User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
```

### **4. Lấy thông tin user trong Controller**

```csharp
public class MyController : Controller
{
    public IActionResult MyAction()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        var accountName = User.FindFirst("AccountName")?.Value;

        // Logic...
    }
}
```

---

## 🎯 Flow hoạt động

### **Đăng nhập**

1. User nhập email/password → `/AdminAuth/Login` (POST)
2. Kiểm tra role (Admin/Staff/Warehouse)
3. Nếu OK → Tạo Claims + Cookie (`AdminAuth`)
4. Redirect → `/Admin/Dashboard`

### **Kiểm tra quyền**

1. User truy cập `/BlogCategory/Manage`
2. Middleware check:
    - Có cookie `AdminAuth`? → Có → Tiếp
    - Role = Admin/Staff? → Có → Cho vào
    - Không → Redirect `/AdminAuth/AccessDenied`

### **Logout**

1. User click "Log out" → SweetAlert2 xác nhận
2. POST `/AdminAuth/Logout`
3. Xóa cookie `AdminAuth`
4. Redirect → `/AdminAuth/Login`

---

## 🔧 Cấu hình đã thêm

### **Program.cs**

```csharp
// Thêm AdminScheme
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, ...)
    .AddCookie("AdminScheme", options =>
    {
        options.LoginPath = "/AdminAuth/Login";
        options.LogoutPath = "/AdminAuth/Logout";
        options.AccessDeniedPath = "/AdminAuth/AccessDenied";
        options.Cookie.Name = "AdminAuth"; // Cookie riêng
    });
```

---

## 📝 TODO (Các tính năng còn thiếu)

-   [ ] Quên mật khẩu Admin (gửi email reset)
-   [ ] Trang `/Admin/Profile` (chỉnh sửa thông tin)
-   [ ] Thay đổi mật khẩu
-   [ ] Activity Log (ghi lại hành động Admin)
-   [ ] 2FA (Two-Factor Authentication)

---

## 🧪 Test thử

### **Test 1: Đăng nhập với role khác**

-   Email: `customer@example.com` (role Customer)
-   Kết quả: ❌ "Bạn không có quyền truy cập vào hệ thống quản lý"

### **Test 2: Warehouse truy cập Blog**

-   URL: `/BlogCategory/Manage`
-   Role: Warehouse
-   Kết quả: ❌ Redirect `/AdminAuth/AccessDenied`

### **Test 3: Staff truy cập Blog**

-   URL: `/BlogCategory/Manage`
-   Role: Staff
-   Kết quả: ✅ Cho phép

### **Test 4: Customer và Admin cùng đăng nhập**

-   Tab 1: Đăng nhập Customer → Cookie `.AspNetCore.Cookies`
-   Tab 2: Đăng nhập Admin → Cookie `AdminAuth`
-   Kết quả: ✅ Cả 2 hoạt động độc lập

---

## 📌 Lưu ý quan trọng

1. **Luôn dùng `[Authorize(AuthenticationSchemes = "AdminScheme")]`** cho controller admin
2. **Không dùng `[Authorize]` đơn thuần** (sẽ dùng CustomerScheme mặc định)
3. **AdminScheme và CustomerScheme độc lập hoàn toàn**
4. **Claims quan trọng**:
    - `NameIdentifier` → User ID
    - `Name` → Email
    - `Role` → Admin/Staff/Warehouse
    - `AccountName` → Tên hiển thị
    - `Avatar` → Đường dẫn ảnh đại diện

---

## 🎨 Giao diện

### Login Page

-   Gradient background (purple theme)
-   Icon shield
-   Form tối giản
-   SweetAlert2 notifications

### Access Denied Page

-   Icon warning
-   Thông báo rõ ràng
-   Button quay về Dashboard

### Profile Modal

-   Avatar 100x100px
-   Thông tin cơ bản
-   Button "Chỉnh sửa" → `/Admin/Profile`

---

✅ **Hoàn thành**: Hệ thống đăng nhập Admin đã sẵn sàng!

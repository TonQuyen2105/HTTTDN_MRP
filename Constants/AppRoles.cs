using System;
using System.Security.Claims;

namespace httt.Constants
{
    public static class AppRoles
    {
        public const string Admin = "Admin";
        public const string Production = "Production";
        public const string Warehouse = "Warehouse";
        public const string Sales = "Sales";

        public static readonly string[] All = [Admin, Production, Warehouse, Sales];

        /// <summary>
        /// Chuẩn hóa vai trò từ CSDL về 4 vai trò hệ thống chuẩn
        /// </summary>
        public static string NormalizeRole(string? rawRole)
        {
            if (string.IsNullOrWhiteSpace(rawRole)) return Production;
            var r = rawRole.Trim();

            // Admin
            if (r.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                r.Equals("Quản trị viên", StringComparison.OrdinalIgnoreCase) ||
                r.Equals("Administrator", StringComparison.OrdinalIgnoreCase))
            {
                return Admin;
            }

            // Kho
            if (r.Equals("Warehouse", StringComparison.OrdinalIgnoreCase) ||
                r.Equals("Kho", StringComparison.OrdinalIgnoreCase) ||
                r.Equals("Thủ kho", StringComparison.OrdinalIgnoreCase))
            {
                return Warehouse;
            }

            // Bán hàng
            if (r.Equals("Sales", StringComparison.OrdinalIgnoreCase) ||
                r.Equals("BanHang", StringComparison.OrdinalIgnoreCase) ||
                r.Equals("Bán hàng", StringComparison.OrdinalIgnoreCase) ||
                r.Equals("Kinh doanh", StringComparison.OrdinalIgnoreCase))
            {
                return Sales;
            }

            // Sản xuất (Manager: Quản đốc phân xưởng; Employee: Dược sĩ sản xuất)
            if (r.Equals("Production", StringComparison.OrdinalIgnoreCase) ||
                r.Equals("SanXuat", StringComparison.OrdinalIgnoreCase) ||
                r.Equals("Sản xuất", StringComparison.OrdinalIgnoreCase) ||
                r.Equals("Manager", StringComparison.OrdinalIgnoreCase) ||
                r.Equals("Employee", StringComparison.OrdinalIgnoreCase) ||
                r.Equals("Dược sĩ", StringComparison.OrdinalIgnoreCase))
            {
                return Production;
            }

            return r;
        }

        /// <summary>
        /// Tên hiển thị tiếng Việt trên giao diện
        /// </summary>
        public static string GetDisplayName(string? role)
        {
            var normalized = NormalizeRole(role);
            return normalized switch
            {
                Admin => "Quản trị viên (Admin)",
                Production => "Phân hệ Sản xuất & MRP",
                Warehouse => "Phân hệ Kho dược phẩm",
                Sales => "Phân hệ Bán hàng & Phân phối",
                _ => role ?? "Người dùng"
            };
        }

        /// <summary>
        /// Trang chủ mặc định theo vai trò khi đăng nhập thành công
        /// </summary>
        public static string GetDefaultUrlForRole(string? role)
        {
            var normalized = NormalizeRole(role);
            return normalized switch
            {
                Admin => "/Admin/Dashboard",
                Production => "/Productions/Home/Index",
                Warehouse => "/Warehouse/Dashboard",
                Sales => "/Sales/Orders",
                _ => "/Productions/Home/Index"
            };
        }

        /// <summary>
        /// Trang đăng nhập riêng cho từng phân hệ (Role)
        /// </summary>
        public static string GetLoginUrlForRole(string? role)
        {
            var normalized = NormalizeRole(role);
            return normalized switch
            {
                Admin => "/Admin/Account/Login",
                Sales => "/Sales/Account/Login",
                Production => "/Productions/Account/Login",
                Warehouse => "/Warehouse/Account/Login",
                _ => "/Productions/Account/Login"
            };
        }

        /// <summary>
        /// Xác định trang đăng nhập phù hợp dựa trên đường dẫn URL mà người dùng đang truy cập hoặc vừa đăng xuất
        /// </summary>
        public static string GetLoginUrlForPath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return "/Productions/Account/Login";

            if (path.StartsWith("/Admin", StringComparison.OrdinalIgnoreCase))
                return "/Admin/Account/Login";

            if (path.StartsWith("/Sales", StringComparison.OrdinalIgnoreCase))
                return "/Sales/Account/Login";

            if (path.StartsWith("/Warehouse", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/Areas/Warehouse", StringComparison.OrdinalIgnoreCase))
                return "/Warehouse/Account/Login";

            if (path.StartsWith("/Productions", StringComparison.OrdinalIgnoreCase))
                return "/Productions/Account/Login";

            return "/Productions/Account/Login";
        }

        /// <summary>
        /// Kiểm tra xem người dùng có quyền truy cập URL nội bộ này không
        /// </summary>
        public static bool CanAccessUrl(ClaimsPrincipal? user, string? localUrl)
        {
            if (user?.Identity?.IsAuthenticated != true || string.IsNullOrEmpty(localUrl))
                return false;

            // Chặn chuyển hướng lại chính các trang Login hoặc AccessDenied
            if (localUrl.Contains("/Login", StringComparison.OrdinalIgnoreCase) ||
                localUrl.StartsWith("/Account/AccessDenied", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var role = NormalizeRole(user.FindFirst(ClaimTypes.Role)?.Value);

            if (localUrl.StartsWith("/Admin", StringComparison.OrdinalIgnoreCase))
                return role == Admin;

            if (localUrl.StartsWith("/Productions", StringComparison.OrdinalIgnoreCase))
                return role == Production;

            if (localUrl.StartsWith("/Warehouse", StringComparison.OrdinalIgnoreCase) ||
                localUrl.StartsWith("/Areas/Warehouse", StringComparison.OrdinalIgnoreCase))
                return role == Warehouse;

            if (localUrl.StartsWith("/Sales", StringComparison.OrdinalIgnoreCase))
                return role == Sales;

            return true;
        }
    }
}

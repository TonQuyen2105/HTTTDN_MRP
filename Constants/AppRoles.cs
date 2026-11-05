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
                Production => "Phân hệ Sản xuất",
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
                Admin => "/Admin/Home/Index",
                Production => "/Productions/Home/Index",
                Warehouse => "/Warehouse/Home/Index",
                Sales => "/Sales/Home/Index",
                _ => "/Productions/Home/Index"
            };
        }

        /// <summary>
        /// Kiểm tra xem người dùng có quyền truy cập URL nội bộ này không
        /// </summary>
        public static bool CanAccessUrl(ClaimsPrincipal? user, string? localUrl)
        {
            if (user?.Identity?.IsAuthenticated != true || string.IsNullOrEmpty(localUrl))
                return false;

            // Chặn chuyển hướng lại chính trang Login hoặc AccessDenied
            if (localUrl.StartsWith("/Account/Login", StringComparison.OrdinalIgnoreCase) ||
                localUrl.StartsWith("/Account/AccessDenied", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var role = NormalizeRole(user.FindFirst(ClaimTypes.Role)?.Value);

            // Admin được truy cập cả 4 phân hệ
            if (role == Admin)
                return true;

            if (localUrl.StartsWith("/Admin", StringComparison.OrdinalIgnoreCase))
                return false;

            if (localUrl.StartsWith("/Productions", StringComparison.OrdinalIgnoreCase))
                return role == Production;

            if (localUrl.StartsWith("/Warehouse", StringComparison.OrdinalIgnoreCase))
                return role == Warehouse;

            if (localUrl.StartsWith("/Sales", StringComparison.OrdinalIgnoreCase))
                return role == Sales;

            return true;
        }
    }
}

using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace httt.Constants
{
    public static class AppRoles
    {
        public const string Admin = "Admin";
        public const string Production = "Production";
        public const string Warehouse = "Warehouse";
        public const string Sales = "Sales";

        // ── Authentication Schemes & Cookie Names riêng biệt cho 4 phân hệ ──
        public const string DynamicScheme = "DynamicPortalScheme";
        public const string AdminScheme = "AdminScheme";
        public const string SalesScheme = "SalesScheme";
        public const string ProductionScheme = "ProductionScheme";
        public const string WarehouseScheme = "WarehouseScheme";

        public const string AdminCookieName = "httt.Auth.Admin";
        public const string SalesCookieName = "httt.Auth.Sales";
        public const string ProductionCookieName = "httt.Auth.Production";
        public const string WarehouseCookieName = "httt.Auth.Warehouse";

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
        /// Lấy Authentication Scheme tương ứng với vai trò của phân hệ
        /// </summary>
        public static string GetSchemeForRole(string? role)
        {
            var normalized = NormalizeRole(role);
            return normalized switch
            {
                Admin => AdminScheme,
                Sales => SalesScheme,
                Production => ProductionScheme,
                Warehouse => WarehouseScheme,
                _ => ProductionScheme
            };
        }

        /// <summary>
        /// Thử xác định Authentication Scheme dựa trên đường dẫn URL
        /// </summary>
        public static string? TryGetSchemeForPath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            if (path.StartsWith("/Admin", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/TestDb", StringComparison.OrdinalIgnoreCase))
                return AdminScheme;

            if (path.StartsWith("/Sales", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/Account/LoginSales", StringComparison.OrdinalIgnoreCase))
                return SalesScheme;

            if (path.StartsWith("/Productions", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/Account/LoginProduction", StringComparison.OrdinalIgnoreCase))
                return ProductionScheme;

            if (path.StartsWith("/Warehouse", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/Areas/Warehouse", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/api/warehouse", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/api/production-requests", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/Account/LoginWarehouse", StringComparison.OrdinalIgnoreCase))
                return WarehouseScheme;

            return null;
        }

        /// <summary>
        /// Xác định Authentication Scheme động cho HttpContext hiện tại (cho phép 4 tab của 4 phân hệ chạy song song)
        /// </summary>
        public static string ResolveSchemeForContext(HttpContext context)
        {
            var reqPath = context.Request.Path.Value ?? string.Empty;

            // 1. Ưu tiên xác định trực tiếp từ đường dẫn URL của phân hệ
            var schemeFromPath = TryGetSchemeForPath(reqPath);
            if (schemeFromPath != null)
                return schemeFromPath;

            // 2. Nếu là các route dùng chung (/Account/Login, /Account/Logout, /Account/AccessDenied...)
            // thì kiểm tra query string (area, portal, returnUrl)
            var areaParam = context.Request.Query["area"].ToString();
            if (string.IsNullOrWhiteSpace(areaParam))
                areaParam = context.Request.Query["portal"].ToString();

            if (!string.IsNullOrWhiteSpace(areaParam))
            {
                if (areaParam.Equals("Admin", StringComparison.OrdinalIgnoreCase))
                    return AdminScheme;
                if (areaParam.Equals("Sales", StringComparison.OrdinalIgnoreCase))
                    return SalesScheme;
                if (areaParam.Equals("Warehouse", StringComparison.OrdinalIgnoreCase))
                    return WarehouseScheme;
                if (areaParam.Equals("Production", StringComparison.OrdinalIgnoreCase) ||
                    areaParam.Equals("Productions", StringComparison.OrdinalIgnoreCase))
                    return ProductionScheme;
            }

            var returnUrlParam = context.Request.Query["returnUrl"].ToString();
            var schemeFromReturnUrl = TryGetSchemeForPath(returnUrlParam);
            if (schemeFromReturnUrl != null)
                return schemeFromReturnUrl;

            // 3. Kiểm tra Referer header (ví dụ khi gọi từ trang của phân hệ nào đó)
            var referer = context.Request.Headers.Referer.ToString();
            if (!string.IsNullOrWhiteSpace(referer) && Uri.TryCreate(referer, UriKind.Absolute, out var refUri))
            {
                var schemeFromReferer = TryGetSchemeForPath(refUri.AbsolutePath);
                if (schemeFromReferer != null)
                    return schemeFromReferer;
            }

            // 4. Đối với trang gốc (/) hoặc các đường dẫn chung, ưu tiên Cookie đang có sẵn
            if (context.Request.Cookies.ContainsKey(ProductionCookieName))
                return ProductionScheme;
            if (context.Request.Cookies.ContainsKey(SalesCookieName))
                return SalesScheme;
            if (context.Request.Cookies.ContainsKey(WarehouseCookieName))
                return WarehouseScheme;
            if (context.Request.Cookies.ContainsKey(AdminCookieName))
                return AdminScheme;

            return ProductionScheme;
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

            if (path.StartsWith("/Admin", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/TestDb", StringComparison.OrdinalIgnoreCase))
                return "/Admin/Account/Login";

            if (path.StartsWith("/Sales", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/Account/LoginSales", StringComparison.OrdinalIgnoreCase))
                return "/Sales/Account/Login";

            if (path.StartsWith("/Warehouse", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/Areas/Warehouse", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/api/warehouse", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/api/production-requests", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/Account/LoginWarehouse", StringComparison.OrdinalIgnoreCase))
                return "/Warehouse/Account/Login";

            if (path.StartsWith("/Productions", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/Account/LoginProduction", StringComparison.OrdinalIgnoreCase))
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

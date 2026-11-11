namespace httt.Areas.Admin.Services;

public enum ServiceError
{
    None,
    Validation,
    NotFound,
    /// <summary>Vi phạm ràng buộc nghiệp vụ (Admin cuối cùng, trùng username, dữ liệu liên quan...).</summary>
    Conflict,
    /// <summary>Hành động bị cấm với chính người thực hiện (tự khóa, tự xóa...).</summary>
    Forbidden
}

/// <summary>Kết quả chuẩn của mọi thao tác ghi. Không bao giờ chứa dữ liệu nhạy cảm (hash mật khẩu...).</summary>
public sealed record ServiceResult(
    bool Success,
    string Message,
    ServiceError Error = ServiceError.None,
    string? Field = null,
    string? Value = null,
    int Affected = 0)
{
    public static ServiceResult Ok(string message, string? value = null, int affected = 0)
        => new(true, message, ServiceError.None, null, value, affected);

    public static ServiceResult Fail(ServiceError error, string message, string? field = null)
        => new(false, message, error, field);
}

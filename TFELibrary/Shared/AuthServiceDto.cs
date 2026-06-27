using System.ComponentModel.DataAnnotations;

namespace TFELibrary.Shared;

public class LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public OperationResult Error { get; set; } = new OperationResult(false, string.Empty);
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}

public class RegisterRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;
}

public class RegisterResponse
{
    public OperationResult Error { get; set; } = new OperationResult(false, string.Empty);
}

public class RefreshTokenRequest
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}

public class RefreshTokenResponse
{
    public OperationResult Error { get; set; } = new OperationResult(false, string.Empty);
}

public enum BulkUserActionType
{
    Suspend,
    Delete,
    Unsuspend
}

public class UserRoleUpdateRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public RoleType NewRole { get; set; }
}

public class UserRoleUpdateResponse
{
    public OperationResult Error { get; set; } = new OperationResult(false, string.Empty);
}

public class BulkUserImportRequest
{
    [Required]
    public string FileName { get; set; } = string.Empty;

    [Required]
    public byte[] FileContent { get; set; } = Array.Empty<byte>();
}

public class BulkUserImportResponse
{
    public OperationResult Error { get; set; } = new OperationResult(false, string.Empty);
    public int CreatedCount { get; set; }
    public int SkippedCount { get; set; }
}

public class BulkUserActionRequest
{
    [Required]
    public List<string> UserIds { get; set; } = new();

    [Required]
    public BulkUserActionType Action { get; set; }
}

public class BulkUserActionResponse
{
    public OperationResult Error { get; set; } = new OperationResult(false, string.Empty);
    public int AffectedCount { get; set; }
}

public class AdminPasswordChangeRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class AdminPasswordChangeResponse
{
    public OperationResult Error { get; set; } = new OperationResult(false, string.Empty);
}

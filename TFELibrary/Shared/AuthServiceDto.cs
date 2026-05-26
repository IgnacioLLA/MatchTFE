using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace TFELibrary.Shared
{

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
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
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
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int ImportedCount { get; set; }
    }

    public enum BulkUserActionType
    {
        Suspend,
        Delete
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
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int AffectedCount { get; set; }
    }
}

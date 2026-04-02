using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace TFELibrary.Shared
{
    public class ChangeRoleDto
    {
        [Required]
        public string NewRole { get; set; } = string.Empty;
    }
}

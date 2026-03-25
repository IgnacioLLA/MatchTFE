using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace TFELibrary.Data
{
    public class TeacherProfile : UserProfile
    {
        [Required]
        [MaxLength(150)]
        public string Department { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? OfficeLocation { get; set; }
    }
}

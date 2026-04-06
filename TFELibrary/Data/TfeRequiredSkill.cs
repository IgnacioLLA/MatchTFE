using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace TFELibrary.Data
{
    public class TfeRequiredSkill
    {
        [Required]
        public int TfeId { get; set; }
        [Required]
        public TFE Tfe { get; set; } = null!;
        [Required]
        public int TagId { get; set; }
        [Required]
        public Tag Tag { get; set; } = null!;

        [Range(1, 5)]
        public int Level { get; set; }
    }
}

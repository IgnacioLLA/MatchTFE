using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace TFELibrary.Data
{
    public class StudentProfile : UserProfile
    {

        public ICollection<StudentSkill> Skills { get; set; } = new List<StudentSkill>();
    }
}

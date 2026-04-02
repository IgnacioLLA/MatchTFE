using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace TFELibrary.Shared
{

    public class TagCreationRequest
    {
        [MaxLength(50)]
        public TagDto Tag { get; set; }
    }
}

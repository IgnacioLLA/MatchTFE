using System;
using System.Collections.Generic;
using System.Text;

namespace TFELibrary.Data
{
    public class UserInterest
    {
        public int InterestsId { get; set; }
        public Tag Interest { get; set; } = null!;
        public string UserProfileUserId { get; set; } = string.Empty;
        public UserProfile UserProfile { get; set; } = null!;
    }
}

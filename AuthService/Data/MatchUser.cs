using Microsoft.AspNetCore.Identity;

namespace AuthService.Data
{
    public class MatchUser : IdentityUser
    {
        public string Name { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;
    }
}

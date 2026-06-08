using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Data
{
    public class AuthDbContext : IdentityDbContext<MatchUser>
    {
        public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<MatchUser>()
                .HasIndex(user => user.NormalizedEmail)
                .IsUnique()
                .HasDatabaseName("EmailIndex");
        }
    }
}

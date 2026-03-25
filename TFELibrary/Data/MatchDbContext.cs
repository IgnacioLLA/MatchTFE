using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TFELibrary.Data;

namespace AuthService.Data
{
    public class MatchDbContext : IdentityDbContext<MatchUser>
    {
        public MatchDbContext(DbContextOptions<MatchDbContext> options) : base(options) { }
        public DbSet<UserProfile> UserProfile { get; set; }
        public DbSet<StudentProfile> StudentProfile { get; set; }
        public DbSet<TeacherProfile> TeacherProfile { get; set; }
        public DbSet<Tag> Tag { get; set; }
        public DbSet<StudentSkill> StudentSkill { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<UserProfile>()
                .HasOne<MatchUser>()
                .WithOne()
                .HasForeignKey<UserProfile>(p => p.UserId)
                .HasPrincipalKey<MatchUser>(u => u.Id)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<StudentSkill>()
                .HasKey(ss => new { ss.StudentProfileId, ss.TagId });

            builder.Entity<StudentProfile>().ToTable("StudentProfiles");
            builder.Entity<TeacherProfile>().ToTable("TeacherProfiles");
        }
        
    }
}

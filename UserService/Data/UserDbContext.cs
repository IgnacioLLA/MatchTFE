using Microsoft.EntityFrameworkCore;

using TFELibrary.Data;



namespace UserService.Data
{
    public class UserDbContext : DbContext
    {
        public UserDbContext(DbContextOptions<UserDbContext> options) : base(options) { }

        public DbSet<UserProfile> UserProfile { get; set; }
        public DbSet<StudentSkill> StudentSkill { get; set; }
        public DbSet<Tag> Tag { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<UserProfile>().ToTable("UserProfile");
            builder.Entity<StudentSkill>().ToTable("StudentSkill");
            builder.Entity<Tag>().ToTable("Tag");

            builder.Entity<UserProfile>()
                .HasIndex(p => p.UserId)
                .IsUnique();

            // Foreign key relationship between UserProfile and MatchUser (from AuthService)
            builder.Entity<StudentSkill>()
                .HasKey(ss => new { ss.StudentProfileId, ss.TagId });

            builder.Entity<UserProfile>()
               .Property(e => e.Role)
               .HasConversion<string>();
        }
    }
}
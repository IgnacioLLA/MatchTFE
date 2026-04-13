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
        public DbSet<UserInterest> UserInterest { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<UserProfile>().ToTable("UserProfile");
            builder.Entity<StudentSkill>().ToTable("StudentSkill");
            builder.Entity<Tag>().ToTable("Tag", t => t.ExcludeFromMigrations());
            builder.Entity<TFEProposal>().ToTable("TfeProposal", t => t.ExcludeFromMigrations());

            builder.Entity<TFEProposal>().Ignore(tp => tp.Tfe);
            //builder.Entity<UserProfile>().Ignore(u => u.StudentSkills);
            //builder.Entity<StudentSkill>().Ignore(ss => ss.Tag);

            builder.Entity<UserProfile>()
                .HasIndex(p => p.UserId)
                .IsUnique();

            builder.Entity<UserProfile>()
                .Property(e => e.Role)
                .HasConversion<string>();

            builder.Entity<StudentSkill>(ss =>
            {
                ss.HasKey(ss => new { ss.StudentProfileId, ss.TagId });

                // Comment this if rebuilding the BD (Cross context-error)
                ss.HasOne(s => s.Tag)
                  .WithMany()
                  .HasForeignKey(s => s.TagId)
                  .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<UserInterest>(j =>
            {
                j.ToTable("UserInterest");
                j.HasKey(ui => new { ui.TagId, ui.UserProfileId });

                j.HasOne(ui => ui.UserProfile)
                 .WithMany(u => u.UserInterests) 
                 .HasForeignKey(ui => ui.UserProfileId)
                 .OnDelete(DeleteBehavior.Cascade);

                // Comment this if rebuilding the BD (Cross context-error)
                j.HasOne(ui => ui.Tag)
                 .WithMany()
                 .HasForeignKey(ui => ui.TagId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<TFEProposal>().HasKey(tp => new { tp.OriginUserId, tp.TfeId });
            builder.Entity<TFEProposal>()
                .HasOne(tp => tp.OriginUser)
                .WithMany(u => u.TfeProposals)
                .HasForeignKey(tp => tp.OriginUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
using Microsoft.EntityFrameworkCore;
using TFELibrary.Data;

namespace MatchService.Data
{
    public class MatchDbContext : DbContext
    {
        public MatchDbContext(DbContextOptions<MatchDbContext> options) : base(options) {
        }
        
        // ==========================================
        // MAIN ENTITIES
        // ==========================================
        public DbSet<TFE> Tfe { get; set; }
        public DbSet<TFEProposal> TfeProposal { get; set; }
        public DbSet<InterestProposal> InterestProposal { get; set; }
        public DbSet<Tag> Tag { get; set; }
        public DbSet<TfeTopic> TfeTopic { get; set; }
        public DbSet<TfeRequiredSkill> TfeRequiredSkill { get; set; }


        // ==========================================
        //  FOREIGN ENTITIES
        // ==========================================
        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<UserInterest> UserInterest { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Tag>().ToTable("Tag");
            builder.Entity<Tag>()
                .HasIndex(t => t.Name)
                .IsUnique();

            builder.Entity<UserProfile>().ToTable("UserProfile");

            builder.Entity<UserProfile>().Ignore(u => u.UserInterests);
            builder.Entity<UserProfile>().Ignore(u => u.StudentSkills);

            builder.Entity<UserInterest>(j =>
            {
                j.ToTable("UserInterest", t => t.ExcludeFromMigrations());
                j.HasKey(ui => new { ui.TagId, ui.UserProfileId });
            });

            // EF needs to know about the table between UserProfile and Tag, even if it's not responsible for managing it
            builder.Entity<TFE>()
                .HasMany(t => t.Topics)
                .WithMany()
                .UsingEntity<TfeTopic>(
                    j => j.HasOne(pt => pt.Tag).WithMany().HasForeignKey(pt => pt.TagId),
                    j => j.HasOne(pt => pt.Tfe).WithMany().HasForeignKey(pt => pt.TfeId),
                    j =>
                    {
                        j.ToTable("TfeTopic");
                        j.HasKey(t => new { t.TfeId, t.TagId });
                    });

            builder.Entity<TFEProposal>()
                .HasOne(tp => tp.Tfe)
                .WithMany(t => t.Proposals)
                .HasForeignKey(tp => tp.TfeId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<TfeRequiredSkill>(j =>
            {
                j.ToTable("TfeRequiredSkill");
                j.HasKey(rs => new { rs.TfeId, rs.TagId });

                j.HasOne(rs => rs.Tfe)
                 .WithMany(t => t.RequiredSkills)
                 .HasForeignKey(rs => rs.TfeId)
                 .OnDelete(DeleteBehavior.Cascade);

                j.HasOne(rs => rs.Tag)
                 .WithMany()
                 .HasForeignKey(rs => rs.TagId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<InterestProposal>()
                .HasKey(ip => new { ip.OriginUserId, ip.DestinationUserId });

            builder.Entity<TFEProposal>()
                .HasKey(tp => new { tp.OriginUserId, tp.TfeId });

            builder.Entity<InterestProposal>()
                .HasOne(ip => ip.OriginUser).WithMany().HasForeignKey(ip => ip.OriginUserId).OnDelete(DeleteBehavior.Cascade);

            builder.Entity<InterestProposal>()
                .HasOne(ip => ip.DestinationUser).WithMany().HasForeignKey(ip => ip.DestinationUserId).OnDelete(DeleteBehavior.Cascade);

            builder.Entity<TFEProposal>()
                .HasOne(tp => tp.OriginUser).WithMany().HasForeignKey(tp => tp.OriginUserId).OnDelete(DeleteBehavior.Cascade);



            // ENUM CONVERSIONS
            builder.Entity<UserProfile>().Property(u => u.Role).HasConversion<string>();
            //builder.Entity<TFE>().Property(t => t.Status).HasConversion<string>();
            //builder.Entity<InterestProposal>().Property(ip => ip.Status).HasConversion<string>();
            //builder.Entity<TFEProposal>().Property(tp => tp.Status).HasConversion<string>();
        }
    }
}
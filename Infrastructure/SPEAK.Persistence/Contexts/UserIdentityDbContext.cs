using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SPEAK.Domain.Models;
using SPEAK.Domain.Models.Chat;
using SPEAK.Domain.Models.Identity;

namespace SPEAK.Persistence.Contexts
{
    public class UserIdentityDbContext(DbContextOptions<UserIdentityDbContext> options) : IdentityDbContext<ApplicationUser>(options)
    {
        public DbSet<DiagnosticRecord> DiagnosticRecords { get; set; }
        public DbSet<DoctorProfile> DoctorProfiles { get; set; }
        public DbSet<ParentProfile> ParentProfiles { get; set; }
        public DbSet<AdminLog> AdminLogs { get; set; }
        public DbSet<Message> Messages { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ApplicationUser>().ToTable("Users");
            builder.Entity<IdentityRole>().ToTable("Roles");
            builder.Entity<IdentityUserRole<string>>().ToTable("UserRoles");

            // Soft Delete global filter — any query ignores deleted users automatically
            builder.Entity<ApplicationUser>()
                .HasQueryFilter(u => !u.IsDeleted);

            // One-to-one: User <-> DoctorProfile
            builder.Entity<ApplicationUser>()
                .HasOne(u => u.DoctorProfile)
                .WithOne(d => d.User)
                .HasForeignKey<DoctorProfile>(d => d.UserId);

            // One-to-one: User <-> ParentProfile
            builder.Entity<ApplicationUser>()
                .HasOne(u => u.ParentProfile)
                .WithOne(p => p.User)
                .HasForeignKey<ParentProfile>(p => p.UserId);
        }
    }
}
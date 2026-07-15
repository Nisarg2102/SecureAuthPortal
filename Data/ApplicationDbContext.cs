using Microsoft.EntityFrameworkCore;
using SecureAuthPortal.Models;

namespace SecureAuthPortal.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<UserMaster> UserMaster { get; set; }
        public DbSet<RoleMaster> RoleMaster { get; set; }
        public DbSet<DocumentMaster> DocumentMaster { get; set; }
        public DbSet<ActivityLog> ActivityLog { get; set; }
        public DbSet<ErrorLog> ErrorLog { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure UserMaster
            modelBuilder.Entity<UserMaster>(entity =>
            {
                entity.Property(x => x.UserId)
                    .HasColumnType("bigint");

                entity.Property(x => x.FullName)
                    .HasColumnType("varchar(50)");

                entity.Property(x => x.Username)
                    .HasColumnType("varchar(20)");

                entity.Property(x => x.Password)
                    .HasColumnType("text");

                entity.Property(x => x.EmailId)
                    .HasColumnType("varchar(100)");

                entity.Property(x => x.MobileNo)
                    .HasColumnType("varchar(10)");

                entity.Property(x => x.Gender)
                    .HasColumnType("varchar(10)");

                entity.Property(x => x.RoleId)
                    .HasColumnType("bigint");

                entity.Property(x => x.DOB)
                    .HasColumnType("timestamp without time zone");

                entity.Property(x => x.CreatedDate)
                    .HasColumnType("timestamp without time zone");

                entity.Property(x => x.FailedLoginAttempts)
                    .HasColumnType("integer");

                entity.Property(x => x.LockoutEnd)
                    .HasColumnType("timestamp without time zone");

                entity.HasIndex(u => u.Username).IsUnique();
                entity.HasIndex(u => u.EmailId).IsUnique();
                entity.HasIndex(u => u.MobileNo).IsUnique();

                entity.HasOne(u => u.Role)
                    .WithMany(r => r.Users)
                    .HasForeignKey(u => u.RoleId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure RoleMaster
            modelBuilder.Entity<RoleMaster>(entity =>
            {
                entity.Property(x => x.RoleId)
                    .HasColumnType("bigint");

                entity.Property(x => x.RoleName)
                    .HasColumnType("varchar(50)");

                entity.Property(x => x.Description)
                    .HasColumnType("varchar(200)");

                entity.Property(x => x.CreatedDate)
                    .HasColumnType("timestamp without time zone");

                entity.Property(x => x.ModifiedDate)
                    .HasColumnType("timestamp without time zone");
            });

            // Configure DocumentMaster
            modelBuilder.Entity<DocumentMaster>(entity =>
            {
                entity.Property(x => x.DocumentId)
                    .HasColumnType("bigint");

                entity.Property(x => x.UserId)
                    .HasColumnType("bigint");

                entity.Property(x => x.DocumentType)
                    .HasColumnType("varchar(50)");

                entity.Property(x => x.FileName)
                    .HasColumnType("varchar(100)");

                entity.Property(x => x.ContentType)
                    .HasColumnType("varchar(100)");

                entity.Property(x => x.FileData)
                    .HasColumnType("bytea");

                entity.Property(x => x.DocumentPath)
                    .HasColumnType("varchar(500)");

                entity.Property(x => x.Status)
                    .HasColumnType("varchar(20)");

                entity.Property(x => x.VerificationNotes)
                    .HasColumnType("varchar(500)");

                entity.Property(x => x.UploadDate)
                    .HasColumnType("timestamp without time zone");

                entity.Property(x => x.ApprovedDate)
                    .HasColumnType("timestamp without time zone");

            });

            // Configure the relationship for UploadedDocuments
            modelBuilder.Entity<DocumentMaster>()
                .HasOne(d => d.User)
                .WithMany(u => u.UploadedDocuments)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure the relationship for ApprovedDocuments
            modelBuilder.Entity<DocumentMaster>()
                .HasOne(d => d.ApprovedByUser)
                .WithMany(u => u.ApprovedDocuments)
                .HasForeignKey(d => d.ApprovedBy)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure ActivityLog and ErrorLog to use timestamp without time zone to match application standard
            modelBuilder.Entity<ActivityLog>(entity =>
            {
                entity.Property(x => x.Timestamp)
                    .HasColumnType("timestamp without time zone");
            });

            modelBuilder.Entity<ErrorLog>(entity =>
            {
                entity.Property(x => x.Timestamp)
                    .HasColumnType("timestamp without time zone");
            });
        }
    }
}
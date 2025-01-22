using DemocraticTapON.Models;
using Microsoft.EntityFrameworkCore;

namespace DemocraticTapON.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Bill> Bills { get; set; }
        public DbSet<UserBill> UserBills { get; set; }
        public DbSet<Representative> Representatives { get; set; }
        public DbSet<AccountModel> Accounts { get; set; }
        public DbSet<BillComment> BillComments { get; set; }
        public DbSet<BillDocument> BillDocuments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure UserBill entity
            modelBuilder.Entity<UserBill>()
                .HasKey(ub => new { ub.UserId, ub.BillId });

            modelBuilder.Entity<UserBill>()
                .HasOne(ub => ub.User)
                .WithMany(u => u.UserBill)
                .HasForeignKey(ub => ub.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<UserBill>()
                .HasOne(ub => ub.Bill)
                .WithMany(b => b.UserBills)
                .HasForeignKey(ub => ub.BillId)
                .OnDelete(DeleteBehavior.NoAction);

            // Configure Account entity
            modelBuilder.Entity<AccountModel>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Username).IsUnique();
                entity.Property(e => e.Username).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Password).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Email).HasMaxLength(256).IsRequired();
                entity.Property(e => e.IsEmailVerified).HasDefaultValue(false);
                entity.Property(e => e.LastVerificationDate).IsRequired(false);
            });

            // Configure User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.UserId);
                entity.Property(e => e.FirstName).HasMaxLength(50);
                entity.Property(e => e.LastName).HasMaxLength(50);
                entity.Property(e => e.PhoneNumber).HasMaxLength(20);

                entity.HasOne(u => u.Account)
                    .WithOne(a => a.User)
                    .HasForeignKey<User>(u => u.AccountId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // Configure Representative relationship
            modelBuilder.Entity<Representative>()
                .HasOne(r => r.User)
                .WithOne()
                .HasForeignKey<Representative>(r => r.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            // Configure Bill relationships
            modelBuilder.Entity<Bill>()
                .HasOne(b => b.ProposedByRep)
                .WithMany(r => r.ProposedBills)
                .HasForeignKey(b => b.ProposedByRepId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Bill>()
                .HasMany(b => b.Documents)
                .WithOne(d => d.Bill)
                .HasForeignKey(d => d.BillId)
                .OnDelete(DeleteBehavior.NoAction);

            // Configure BillComment relationships
            modelBuilder.Entity<BillComment>()
                .HasOne(bc => bc.Bill)
                .WithMany(b => b.Comments)
                .HasForeignKey(bc => bc.BillId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<BillComment>()
                .HasOne(bc => bc.User)
                .WithMany()
                .HasForeignKey(bc => bc.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<BillComment>()
                .HasOne(bc => bc.ParentComment)
                .WithMany(bc => bc.Replies)
                .HasForeignKey(bc => bc.ParentCommentId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}

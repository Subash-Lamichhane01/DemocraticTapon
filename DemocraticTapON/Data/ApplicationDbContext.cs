using DemocraticTapON.Models;
using DemocraticTapON.Models.DemocraticTapON.Models;
using Microsoft.EntityFrameworkCore;

namespace DemocraticTapON.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSets representing your tables in the database
        public DbSet<User> Users { get; set; } // Adding User DbSet
        public DbSet<Bill> Bills { get; set; } // Adding Bill DbSet
        public DbSet<Vote> Votes { get; set; }
        public DbSet<UserBill> UserBills { get; set; } // Adding UserBill DbSet
        public DbSet<Comment> Comments { get; set; } // Adding Comment DbSet
        public DbSet<Representative> Representatives { get; set; }
        

        // Override OnModelCreating to configure relationships and other entity configurations
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuring relationships, keys, etc. if necessary
            modelBuilder.Entity<UserBill>()
                .HasKey(ub => new { ub.UserId, ub.BillId });

            modelBuilder.Entity<UserBill>()
                .HasOne(ub => ub.User)
                .WithMany(u => u.UserBill)
                .HasForeignKey(ub => ub.UserId);

            modelBuilder.Entity<UserBill>()
                .HasOne(ub => ub.Bill)
                .WithMany(b => b.UserBill)
                .HasForeignKey(ub => ub.BillId);
        }
        public DbSet<DemocraticTapON.Models.AccountModel> LoginModel { get; set; } = default!;
    }
}

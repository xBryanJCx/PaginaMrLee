using Microsoft.EntityFrameworkCore;
using MrLee.Web.Models;

namespace MrLee.Web.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<ActionLog> ActionLogs => Set<ActionLog>();

    public DbSet<Product> Products => Set<Product>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderStatusHistory> OrderStatusHistory => Set<OrderStatusHistory>();

    public DbSet<OperatingIncome> OperatingIncomes => Set<OperatingIncome>();
    public DbSet<OperatingIncomeAttachment> OperatingIncomeAttachments => Set<OperatingIncomeAttachment>();
    public DbSet<AccountingPeriod> AccountingPeriods => Set<AccountingPeriod>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppUser>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<Role>()
            .HasIndex(r => r.Name)
            .IsUnique();

        modelBuilder.Entity<Permission>()
            .HasIndex(p => p.Code)
            .IsUnique();

        modelBuilder.Entity<RolePermission>()
            .HasKey(rp => new { rp.RoleId, rp.PermissionId });

        modelBuilder.Entity<Product>()
            .HasIndex(p => p.Sku)
            .IsUnique();

        modelBuilder.Entity<Order>()
            .HasIndex(o => o.TrackingNumber)
            .IsUnique();

        modelBuilder.Entity<OperatingIncome>()
            .HasIndex(i => i.Number)
            .IsUnique();

        modelBuilder.Entity<AccountingPeriod>()
            .HasIndex(p => new { p.Year, p.Month })
            .IsUnique();

        // Prevent cascade delete surprises
        modelBuilder.Entity<OrderItem>()
            .HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StockMovement>()
            .HasOne(m => m.Product)
            .WithMany(p => p.Movements)
            .HasForeignKey(m => m.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<OperatingIncomeAttachment>()
            .HasOne(a => a.OperatingIncome)
            .WithMany(i => i.Attachments)
            .HasForeignKey(a => a.OperatingIncomeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
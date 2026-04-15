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

    public DbSet<Puesto> Puestos => Set<Puesto>();
    public DbSet<Sucursal> Sucursales => Set<Sucursal>();
    public DbSet<Empleado> Empleados => Set<Empleado>();
    public DbSet<SolicitudVacacion> SolicitudesVacacion => Set<SolicitudVacacion>();

    public DbSet<Incapacidad> Incapacidades => Set<Incapacidad>();
    public DbSet<DocumentoExpediente> DocumentosExpediente => Set<DocumentoExpediente>();
    public DbSet<DireccionEmpleado> DireccionesEmpleado => Set<DireccionEmpleado>();
    public DbSet<ContactoEmergencia> ContactosEmergencia => Set<ContactoEmergencia>();
    public DbSet<CuentaBancaria> CuentasBancarias => Set<CuentaBancaria>();
    public DbSet<MovimientoLaboral> MovimientosLaborales => Set<MovimientoLaboral>();

    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<DireccionCliente> DireccionesCliente => Set<DireccionCliente>();

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

        //  Empleados 
        modelBuilder.Entity<Empleado>()
            .HasIndex(e => e.Identificacion).IsUnique();
        modelBuilder.Entity<Empleado>()
            .HasIndex(e => e.Codigo).IsUnique();
        modelBuilder.Entity<Empleado>()
            .HasOne(e => e.Puesto).WithMany()
            .HasForeignKey(e => e.PuestoId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Empleado>()
            .HasOne(e => e.Sucursal).WithMany()
            .HasForeignKey(e => e.SucursalId)
            .OnDelete(DeleteBehavior.Restrict);

        //  Vacaciones 
        modelBuilder.Entity<SolicitudVacacion>()
            .HasOne(s => s.Empleado).WithMany(e => e.SolicitudesVacacion)
            .HasForeignKey(s => s.EmpleadoId)
            .OnDelete(DeleteBehavior.Cascade);

        //  Incapacidades 
        modelBuilder.Entity<Incapacidad>()
            .HasOne(i => i.Empleado).WithMany()
            .HasForeignKey(i => i.EmpleadoId)
            .OnDelete(DeleteBehavior.Cascade);

        //  Documentos expediente 
        modelBuilder.Entity<DocumentoExpediente>()
            .HasOne(d => d.Empleado).WithMany()
            .HasForeignKey(d => d.EmpleadoId)
            .OnDelete(DeleteBehavior.Cascade);

        //  Direcciones 
        modelBuilder.Entity<DireccionEmpleado>()
            .HasOne(d => d.Empleado).WithMany()
            .HasForeignKey(d => d.EmpleadoId)
            .OnDelete(DeleteBehavior.Cascade);

        //  Contactos 
        modelBuilder.Entity<ContactoEmergencia>()
            .HasOne(c => c.Empleado).WithMany()
            .HasForeignKey(c => c.EmpleadoId)
            .OnDelete(DeleteBehavior.Cascade);

        //  Cuentas bancarias 
        modelBuilder.Entity<CuentaBancaria>()
            .HasOne(c => c.Empleado).WithMany()
            .HasForeignKey(c => c.EmpleadoId)
            .OnDelete(DeleteBehavior.Cascade);

        //  Movimientos laborales 
        modelBuilder.Entity<MovimientoLaboral>()
            .HasOne(m => m.Empleado).WithMany()
            .HasForeignKey(m => m.EmpleadoId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<MovimientoLaboral>()
            .HasOne(m => m.PuestoNuevo).WithMany()
            .HasForeignKey(m => m.PuestoIdNuevo)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<MovimientoLaboral>()
            .HasOne(m => m.SucursalNueva).WithMany()
            .HasForeignKey(m => m.SucursalIdNueva)
            .OnDelete(DeleteBehavior.Restrict);
        
        modelBuilder.Entity<Cliente>()
            .HasIndex(c => c.Email).IsUnique();

        modelBuilder.Entity<DireccionCliente>()
            .HasOne(d => d.Cliente)
            .WithMany(c => c.Direcciones)
            .HasForeignKey(d => d.ClienteId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}


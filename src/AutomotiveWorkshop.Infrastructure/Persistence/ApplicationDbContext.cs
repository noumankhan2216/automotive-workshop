using AutomotiveWorkshop.Application.Interfaces;
using AutomotiveWorkshop.Domain.Entities;
using AutomotiveWorkshop.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AutomotiveWorkshop.Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();
    public DbSet<WorkOrderItem> WorkOrderItems => Set<WorkOrderItem>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();
    public DbSet<ServiceCatalogItem> ServiceCatalogItems => Set<ServiceCatalogItem>();
    public DbSet<WorkshopSettings> WorkshopSettings => Set<WorkshopSettings>();
    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Customer>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        builder.Entity<Vehicle>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Customer).WithMany(x => x.Vehicles).HasForeignKey(x => x.CustomerId);
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        builder.Entity<WorkOrder>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.WorkOrderNumber).HasMaxLength(50).IsRequired();
            e.HasOne(x => x.Customer).WithMany(x => x.WorkOrders).HasForeignKey(x => x.CustomerId);
            e.HasOne(x => x.Vehicle).WithMany(x => x.WorkOrders).HasForeignKey(x => x.VehicleId);
            e.HasOne(x => x.Invoice).WithOne(x => x.WorkOrder).HasForeignKey<Invoice>(x => x.WorkOrderId);
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        builder.Entity<WorkOrderItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.Ignore(x => x.LineTotal);
            e.HasOne(x => x.WorkOrder).WithMany(x => x.Items).HasForeignKey(x => x.WorkOrderId);
        });

        builder.Entity<Invoice>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.InvoiceNumber).HasMaxLength(50).IsRequired();
            e.HasOne(x => x.Customer).WithMany(x => x.Invoices).HasForeignKey(x => x.CustomerId);
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        builder.Entity<InvoiceLine>(e =>
        {
            e.HasKey(x => x.Id);
            e.Ignore(x => x.LineTotal);
            e.HasOne(x => x.Invoice).WithMany(x => x.Lines).HasForeignKey(x => x.InvoiceId);
        });

        builder.Entity<ServiceCatalogItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
        });

        builder.Entity<WorkshopSettings>(e => e.HasKey(x => x.Id));
        builder.Entity<NotificationLog>(e => e.HasKey(x => x.Id));
    }
}

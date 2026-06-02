using AutomotiveWorkshop.Domain.Entities;
using AutomotiveWorkshop.Domain.Enums;
using AutomotiveWorkshop.Infrastructure.Identity;
using AutomotiveWorkshop.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AutomotiveWorkshop.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        await db.Database.MigrateAsync();

        foreach (var role in new[] { "Admin", "Manager", "Technician", "Receptionist" })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        if (!await db.WorkshopSettings.AnyAsync())
        {
            db.WorkshopSettings.Add(new WorkshopSettings
            {
                WorkshopName = "Automotive Workshop",
                DefaultTaxRate = 0.10m
            });
        }

        if (!await db.ServiceCatalogItems.AnyAsync())
        {
            db.ServiceCatalogItems.AddRange(
                new ServiceCatalogItem { Name = "Oil Change", DefaultPrice = 49.99m },
                new ServiceCatalogItem { Name = "Brake Inspection", DefaultPrice = 29.99m },
                new ServiceCatalogItem { Name = "Tire Rotation", DefaultPrice = 39.99m },
                new ServiceCatalogItem { Name = "General Diagnostic", DefaultPrice = 89.99m });
        }

        const string adminEmail = "admin@workshop.local";
        if (await userManager.FindByEmailAsync(adminEmail) is null)
        {
            var admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "System Administrator",
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(admin, "Admin123!");
            if (result.Succeeded)
                await userManager.AddToRoleAsync(admin, "Admin");
            else
                logger.LogWarning("Failed to seed admin user: {Errors}",
                    string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        await db.SaveChangesAsync();

        await SeedDemoDataAsync(db);
        await SeedDemoEstimatesAsync(db);
    }

    /// <summary>Idempotent estimate seeding that reuses already-seeded demo customers/vehicles,
    /// so it works even when the rest of the demo data was seeded in a prior run.</summary>
    private static async Task SeedDemoEstimatesAsync(ApplicationDbContext db)
    {
        if (await db.Estimates.AnyAsync(e => e.EstimateNumber.StartsWith("EST-DEMO-")))
            return;

        var vehicles = await db.Vehicles.Include(v => v.Customer).ToListAsync();
        if (vehicles.Count == 0) return;

        Vehicle? Find(string make, string model) =>
            vehicles.FirstOrDefault(v => v.Make == make && v.Model == model);

        var now = DateTime.UtcNow;
        const decimal taxRate = 0.10m;
        var estimates = new List<Estimate>();

        if (Find("Honda", "Civic") is { } civic)
        {
            estimates.Add(new Estimate
            {
                EstimateNumber = "EST-DEMO-0001", CustomerId = civic.CustomerId, VehicleId = civic.Id,
                Status = EstimateStatus.Sent, TaxRate = taxRate,
                CreatedAt = now.AddDays(-1), ValidUntil = now.AddDays(29),
                CustomerNotes = "AC not cooling — please advise.",
                Items =
                {
                    new EstimateItem { Description = "AC System Diagnostic", Quantity = 1, UnitPrice = 89.99m },
                    new EstimateItem { Description = "AC Recharge (R-134a)", Quantity = 1, UnitPrice = 129.99m }
                }
            });
        }

        if (Find("BMW", "330i") is { } bmw)
        {
            estimates.Add(new Estimate
            {
                EstimateNumber = "EST-DEMO-0002", CustomerId = bmw.CustomerId, VehicleId = bmw.Id,
                Status = EstimateStatus.Approved, TaxRate = taxRate,
                CreatedAt = now.AddDays(-3), ValidUntil = now.AddDays(27), ApprovedAt = now.AddDays(-1),
                CustomerNotes = "Approved — schedule for next week.",
                Items =
                {
                    new EstimateItem { Description = "Front Brake Pads & Rotors", Quantity = 1, UnitPrice = 420.00m },
                    new EstimateItem { Description = "Brake Fluid Flush", Quantity = 1, UnitPrice = 79.99m }
                }
            });
        }

        if (Find("Subaru", "Outback") is { } subaru)
        {
            estimates.Add(new Estimate
            {
                EstimateNumber = "EST-DEMO-0003", CustomerId = subaru.CustomerId, VehicleId = subaru.Id,
                Status = EstimateStatus.Draft, TaxRate = taxRate,
                CreatedAt = now.AddHours(-5), ValidUntil = now.AddDays(30),
                Items =
                {
                    new EstimateItem { Description = "Head Gasket Inspection", Quantity = 1, UnitPrice = 150.00m }
                }
            });
        }

        if (estimates.Count > 0)
        {
            db.Estimates.AddRange(estimates);
            await db.SaveChangesAsync();
        }
    }

    private static async Task SeedDemoDataAsync(ApplicationDbContext db)
    {
        if (await db.WorkOrders.AnyAsync(w => w.WorkOrderNumber.StartsWith("WO-DEMO-")))
            return;

        var now = DateTime.UtcNow;
        const decimal taxRate = 0.10m;

        // --- Customers ---
        var john = new Customer { Name = "John Carter", Email = "john.carter@example.com", Phone = "555-0101", Address = "120 Maple Street", CreatedAt = now.AddDays(-45) };
        var maria = new Customer { Name = "Maria Gomez", Email = "maria.gomez@example.com", Phone = "555-0102", Address = "88 Oak Avenue", CreatedAt = now.AddDays(-30) };
        var david = new Customer { Name = "David Lee", Email = "david.lee@example.com", Phone = "555-0103", Address = "47 Birch Road", CreatedAt = now.AddDays(-18) };
        var sarah = new Customer { Name = "Sarah Johnson", Email = "sarah.j@example.com", Phone = "555-0104", Address = "9 Pine Court", CreatedAt = now.AddDays(-6) };
        db.Customers.AddRange(john, maria, david, sarah);

        // --- Vehicles ---
        var camry = new Vehicle { Customer = john, Make = "Toyota", Model = "Camry", Year = 2019, LicensePlate = "ABC-1234", Vin = "JT2BF22K1W0123456", Mileage = 45200, Color = "Silver" };
        var f150 = new Vehicle { Customer = john, Make = "Ford", Model = "F-150", Year = 2021, LicensePlate = "TRK-998", Vin = "1FTFW1E50MFA12345", Mileage = 30100, Color = "Black" };
        var civic = new Vehicle { Customer = maria, Make = "Honda", Model = "Civic", Year = 2020, LicensePlate = "HND-456", Vin = "2HGFC2F69LH512345", Mileage = 28900, Color = "Blue" };
        var bmw = new Vehicle { Customer = david, Make = "BMW", Model = "330i", Year = 2018, LicensePlate = "BMW-777", Vin = "WBA8E9G50JNU12345", Mileage = 61000, Color = "White" };
        var tesla = new Vehicle { Customer = sarah, Make = "Tesla", Model = "Model 3", Year = 2022, LicensePlate = "EV-2022", Vin = "5YJ3E1EA7NF123456", Mileage = 12000, Color = "Red" };
        var subaru = new Vehicle { Customer = sarah, Make = "Subaru", Model = "Outback", Year = 2017, LicensePlate = "SUB-321", Vin = "4S4BSANC8H3201234", Mileage = 78000, Color = "Green" };
        db.Vehicles.AddRange(camry, f150, civic, bmw, tesla, subaru);

        // --- Work orders (varied statuses) ---
        var wo1 = new WorkOrder
        {
            WorkOrderNumber = "WO-DEMO-0001", Customer = john, Vehicle = camry,
            Status = WorkOrderStatus.Paid, OpenedAt = now.AddDays(-9), CompletedAt = now.AddHours(-6),
            CustomerNotes = "Routine maintenance.",
            Items =
            {
                new WorkOrderItem { Description = "Oil Change", Quantity = 1, UnitPrice = 49.99m },
                new WorkOrderItem { Description = "Tire Rotation", Quantity = 1, UnitPrice = 39.99m }
            }
        };

        var wo2 = new WorkOrder
        {
            WorkOrderNumber = "WO-DEMO-0002", Customer = john, Vehicle = f150,
            Status = WorkOrderStatus.InProgress, OpenedAt = now.AddDays(-2),
            CustomerNotes = "Squeaking when braking.",
            Items =
            {
                new WorkOrderItem { Description = "Brake Inspection", Quantity = 1, UnitPrice = 29.99m },
                new WorkOrderItem { Description = "Front Brake Pads", Quantity = 1, UnitPrice = 120.00m }
            }
        };

        var wo3 = new WorkOrder
        {
            WorkOrderNumber = "WO-DEMO-0003", Customer = maria, Vehicle = civic,
            Status = WorkOrderStatus.WaitingParts, OpenedAt = now.AddDays(-4),
            InternalNotes = "Alternator on order.",
            Items =
            {
                new WorkOrderItem { Description = "General Diagnostic", Quantity = 1, UnitPrice = 89.99m },
                new WorkOrderItem { Description = "Alternator Replacement", Quantity = 1, UnitPrice = 250.00m }
            }
        };

        var wo4 = new WorkOrder
        {
            WorkOrderNumber = "WO-DEMO-0004", Customer = david, Vehicle = bmw,
            Status = WorkOrderStatus.Invoiced, OpenedAt = now.AddDays(-5), CompletedAt = now.AddHours(-30),
            Items =
            {
                new WorkOrderItem { Description = "Synthetic Oil Change", Quantity = 1, UnitPrice = 79.99m },
                new WorkOrderItem { Description = "Coolant Flush", Quantity = 1, UnitPrice = 59.99m }
            }
        };

        var wo5 = new WorkOrder
        {
            WorkOrderNumber = "WO-DEMO-0005", Customer = sarah, Vehicle = tesla,
            Status = WorkOrderStatus.Draft, OpenedAt = now.AddHours(-3),
            CustomerNotes = "Tire rotation requested.",
            Items =
            {
                new WorkOrderItem { Description = "Tire Rotation", Quantity = 1, UnitPrice = 39.99m }
            }
        };

        var wo6 = new WorkOrder
        {
            WorkOrderNumber = "WO-DEMO-0006", Customer = sarah, Vehicle = subaru,
            Status = WorkOrderStatus.Paid, OpenedAt = now.AddDays(-7), CompletedAt = now.AddHours(-20),
            Items =
            {
                new WorkOrderItem { Description = "Brake Inspection", Quantity = 1, UnitPrice = 29.99m },
                new WorkOrderItem { Description = "Timing Belt Replacement", Quantity = 1, UnitPrice = 350.00m }
            }
        };

        db.WorkOrders.AddRange(wo1, wo2, wo3, wo4, wo5, wo6);

        // --- Invoices (paid + outstanding) ---
        db.Invoices.AddRange(
            BuildInvoice("INV-DEMO-0001", john, wo1, InvoiceStatus.Paid, taxRate, now.AddHours(-5), now.AddHours(-4)),
            BuildInvoice("INV-DEMO-0002", david, wo4, InvoiceStatus.Sent, taxRate, now.AddHours(-28), null),
            BuildInvoice("INV-DEMO-0003", sarah, wo6, InvoiceStatus.Paid, taxRate, now.AddHours(-18), now.AddHours(-2)));

        await db.SaveChangesAsync();
    }

    private static Invoice BuildInvoice(
        string number, Customer customer, WorkOrder workOrder,
        InvoiceStatus status, decimal taxRate, DateTime issuedAt, DateTime? paidAt)
    {
        var subTotal = workOrder.Items.Sum(i => i.Quantity * i.UnitPrice);
        var taxAmount = Math.Round(subTotal * taxRate, 2);

        return new Invoice
        {
            InvoiceNumber = number,
            Customer = customer,
            WorkOrder = workOrder,
            Status = status,
            SubTotal = subTotal,
            TaxRate = taxRate,
            TaxAmount = taxAmount,
            Total = subTotal + taxAmount,
            IssuedAt = issuedAt,
            DueDate = issuedAt.AddDays(14),
            PaidAt = paidAt,
            Lines = workOrder.Items.Select(i => new InvoiceLine
            {
                Description = i.Description,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        };
    }
}

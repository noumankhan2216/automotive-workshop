namespace AutomotiveWorkshop.Domain.Authorization;

/// <summary>Canonical workshop role names, seeded in <c>DatabaseSeeder</c>.</summary>
public static class Roles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Technician = "Technician";
    public const string Receptionist = "Receptionist";

    /// <summary>Roles allowed to create/edit front-office records (estimates, customers, vehicles, invoices).</summary>
    public const string FrontOffice = "Admin,Manager,Receptionist";

    /// <summary>Roles allowed to work jobs on the shop floor (includes technicians).</summary>
    public const string Shop = "Admin,Manager,Receptionist,Technician";
}

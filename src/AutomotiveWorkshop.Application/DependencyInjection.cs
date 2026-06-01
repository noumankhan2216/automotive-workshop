using AutomotiveWorkshop.Application.Interfaces;
using AutomotiveWorkshop.Application.Services;
using AutomotiveWorkshop.Application.Validators;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace AutomotiveWorkshop.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<CreateCustomerValidator>();

        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IVehicleService, VehicleService>();
        services.AddScoped<IWorkOrderService, WorkOrderService>();
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<IDashboardService, DashboardService>();

        return services;
    }
}

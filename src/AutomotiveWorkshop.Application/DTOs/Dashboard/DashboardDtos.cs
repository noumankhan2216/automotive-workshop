namespace AutomotiveWorkshop.Application.DTOs.Dashboard;

public record DashboardSummaryDto(
    decimal RevenueToday,
    decimal RevenueThisWeek,
    decimal RevenueThisMonth,
    int OpenWorkOrders,
    int CompletedWorkOrdersThisMonth,
    int OutstandingInvoices,
    decimal OutstandingAmount);

public record RevenueChartPointDto(string Label, decimal Amount);

public record TopServiceDto(string ServiceName, int Count, decimal Revenue);

// RealEstate.Host/Infrastructure/HangfireDashboardAuthFilter.cs

using Hangfire.Dashboard;

public sealed class HangfireDashboardAuthFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        var isLocal = httpContext.Request.Host.Host == "localhost" ||
                      httpContext.Request.Host.Host == "127.0.0.1";


        if (isLocal)
        {
            return true;
        }
        return httpContext.User.Identity?.IsAuthenticated == true
            && httpContext.User.IsInRole("Admin");
    }
}
namespace Identity.Domain.Enums;

public enum UserRole
{
    Admin,
    Owner,
    Tenant,
    Broker,
    CompanyManager,
    MaintenanceCompany
}

public static class UserRoleExtensions
{
    public static string ToRoleString(this UserRole role) => role switch
    {
        UserRole.Admin => "admin",
        UserRole.Owner => "owner",
        UserRole.Tenant => "tenant",
        UserRole.Broker => "broker",
        UserRole.CompanyManager => "company_manager",
        UserRole.MaintenanceCompany => "maintenance_company",
        _ => throw new ArgumentException($"Unknown role {role}")
    };
}

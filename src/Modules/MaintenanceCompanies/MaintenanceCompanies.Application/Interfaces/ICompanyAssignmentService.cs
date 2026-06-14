namespace MaintenanceCompanies.Application.Interfaces;

public interface ICompanyAssignmentService
{
    /// <summary>
    /// Picks the best maintenance company for a category + city.
    /// Returns null if none available.
    /// </summary>
    Task<Guid?> FindBestCompanyAsync(Guid categoryId, string city);

    /// <summary>
    /// Increments active counter when a ticket is assigned.
    /// </summary>
    Task IncrementActiveTicketsAsync(Guid companyId);

    /// <summary>
    /// Called when ticket is completed/closed — adjusts cached counters and recalculates rating.
    /// </summary>
    Task OnTicketCompletedAsync(Guid companyId);
}
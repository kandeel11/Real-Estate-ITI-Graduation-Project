using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AI.Infrastructure.Data.Interceptors
{
    public class AuditEntitiesInterceptor : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            var context = eventData.Context;
            if (context == null) return base.SavingChangesAsync(eventData, result, cancellationToken);
            var entries = context.ChangeTracker.Entries<BaseEntity>();
            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Modified)
                {
                    entry.Entity.MarkAsUpdated();
                }
                else if (entry.State == EntityState.Deleted)
                {
                    entry.State = EntityState.Modified; // Soft delete: mark as modified to update the DeletedAt field
                    entry.Entity.MarkAsDeleted();
                }
            }
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }
}

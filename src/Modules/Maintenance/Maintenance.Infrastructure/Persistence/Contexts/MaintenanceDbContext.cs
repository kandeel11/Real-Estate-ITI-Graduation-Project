using Maintenance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maintenance.Infrastructure.Persistence.Contexts
{
    public class MaintenanceDbContext : DbContext
    {
        public MaintenanceDbContext(DbContextOptions<MaintenanceDbContext> options) : base(options) { }

        public DbSet<MaintenanceTicket> MaintenanceTickets => Set<MaintenanceTicket>();
        public DbSet<MaintenanceTicketAttachment> MaintenanceTicketAttachments => Set<MaintenanceTicketAttachment>();
        public DbSet<MaintenanceTicketComment> MaintenanceTicketComments => Set<MaintenanceTicketComment>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("maintenance");
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(MaintenanceDbContext).Assembly);
            base.OnModelCreating(modelBuilder);
        }
    }
}

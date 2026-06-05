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
        public DbSet<MaintenanceTicketAttachment> Attachments => Set<MaintenanceTicketAttachment>();
        public DbSet<MaintenanceTicketComment> Comments => Set<MaintenanceTicketComment>();
        public DbSet<TicketAiEvaluation> AiEvaluations => Set<TicketAiEvaluation>();
        public DbSet<TicketVendorAssignment> VendorAssignments => Set<TicketVendorAssignment>();
        public DbSet<TicketStatusHistory> StatusHistory => Set<TicketStatusHistory>();
        public DbSet<MaintenancePayment> Payments => Set<MaintenancePayment>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("maintenance");
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(MaintenanceDbContext).Assembly);
            base.OnModelCreating(modelBuilder);
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maersk.Integrations.EventLog.EF
{
    public class IntegrationEventLogContext : DbContext
    {
        public IntegrationEventLogContext(DbContextOptions<IntegrationEventLogContext> options)
        : base(options)
        {

        }

        public DbSet<IntegrationEventLog> IntegrationEventLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<IntegrationEventLog>(ConfigureIntegrationEventLogEntry);
        }

        private void ConfigureIntegrationEventLogEntry(EntityTypeBuilder<IntegrationEventLog> builder)
        {
            builder.ToTable(nameof(IntegrationEventLog));

            builder.HasKey(d => d.EventId);
            builder.Property(d => d.EventId)
                .IsRequired();
            builder.Property(d => d.Content)
                .IsRequired();
            builder.Property(d => d.CreationTime)
                .IsRequired();
            builder.Property(d => d.State)
                .IsRequired();
            builder.Property(d => d.TimesSent)
                .IsRequired();
            builder.Property(d => d.EventTypeName)
                .IsRequired();
        }
    }
}

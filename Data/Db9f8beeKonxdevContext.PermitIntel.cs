using KonXProWebApp.Models.PermitIntel;
using KonXProWebApp.Models.db_9f8bee_konxdev;
using Microsoft.EntityFrameworkCore;

namespace KonXProWebApp.Data;

public partial class db_9f8bee_konxdevContext
{
    public DbSet<Subscription> Subscriptions { get; set; }

    public DbSet<SavedLead> SavedLeads { get; set; }

    public DbSet<AlertPreference> AlertPreferences { get; set; }

    public DbSet<IngestionLog> IngestionLogs { get; set; }

    public DbSet<HpdViolation> HpdViolations { get; set; }

    partial void OnModelBuilding(ModelBuilder builder)
    {
        builder.Entity<Subscription>(entity =>
        {
            entity.Property(p => p.StartDate).HasColumnType("datetime2");
            entity.Property(p => p.EndDate).HasColumnType("datetime2");
            entity.Property(p => p.TrialEndDate).HasColumnType("datetime2");
            entity.Property(p => p.CreatedAt).HasColumnType("datetime2");
            entity.Property(p => p.UpdatedAt).HasColumnType("datetime2");
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.StripeSubscriptionId).IsUnique().HasFilter("[StripeSubscriptionId] IS NOT NULL");
        });

        builder.Entity<SavedLead>(entity =>
        {
            entity.Property(p => p.SavedAt).HasColumnType("datetime2");
            entity.Property(p => p.UpdatedAt).HasColumnType("datetime2");
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => new { e.UserId, e.DobjobFilingId }).IsUnique();
        });

        builder.Entity<AlertPreference>(entity =>
        {
            entity.Property(p => p.CreatedAt).HasColumnType("datetime2");
            entity.HasIndex(e => e.UserId);
        });

        builder.Entity<IngestionLog>(entity =>
        {
            entity.Property(p => p.RunDate).HasColumnType("datetime2");
            entity.Property(p => p.LastSocrataTimestamp).HasColumnType("datetime2");
        });
    }
}

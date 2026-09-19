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

        builder.Entity<ServiceRequest311>(entity =>
        {
            entity.Property(p => p.CreatedDate).HasColumnType("datetime2");
            entity.Property(p => p.ClosedDate).HasColumnType("datetime2");
            entity.HasIndex(e => e.Bbl);
        });

        builder.Entity<DobViolation>(entity =>
        {
            // dbo.DobBisViolations is a synonym pointing at
            // konx_admin.DobBisViolations, not a real dbo-owned table -
            // synonyms cannot take DDL (ALTER TABLE/CREATE INDEX fail
            // outright against one, even though DML resolves through it
            // fine). Excluding from migrations means a future model change
            // here can never scaffold a migration that hard-fails against
            // production. See REMEDIATION.md's production rebuild scoping.
            entity.ToTable("DobBisViolations", "dbo", tb => tb.ExcludeFromMigrations());

            // Value converter for Id (isn_dob_bis_viol varchar(20) in DB -
            // confirmed via schema-truth; bare "varchar" would mean varchar(1))
            entity.Property(e => e.Id)
                .HasConversion(
                    v => v.ToString(),
                    v => ConvertBoroToInt(v)
                )
                .HasColumnType("varchar(20)");

            // Value converter for IssueDate (char(8) YYYYMMDD in DB)
            entity.Property(e => e.IssueDate)
                .HasConversion(
                    v => v.ToString("yyyyMMdd"),
                    v => System.DateTime.ParseExact(v.Trim(), "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture)
                )
                .HasColumnType("char(8)");

            // Value converter for Boro (varchar(5) in DB - confirmed via
            // schema-truth; bare "varchar" would mean varchar(1))
            entity.Property(e => e.Boro)
                .HasConversion(
                    v => v.ToString(),
                    v => ConvertBoroToInt(v)
                )
                .HasColumnType("varchar(5)");
        });
    }

    private static int ConvertBoroToInt(string v)
    {
        return int.TryParse(v, out var result) ? result : 0;
    }
}

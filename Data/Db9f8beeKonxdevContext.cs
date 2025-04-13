using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using KonXProWebApp.Models.db_9f8bee_konxdev;

namespace KonXProWebApp.Data
{
    public partial class db_9f8bee_konxdevContext : DbContext
    {
        public db_9f8bee_konxdevContext()
        {
        }

        public db_9f8bee_konxdevContext(DbContextOptions<db_9f8bee_konxdevContext> options) : base(options)
        {
        }

        partial void OnModelBuilding(ModelBuilder builder);

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<KonXProWebApp.Models.db_9f8bee_konxdev.VwBasicTierDashboard>().HasNoKey();

            builder.Entity<KonXProWebApp.Models.db_9f8bee_konxdev.VwDemoDisplay>().HasNoKey();

            builder.Entity<KonXProWebApp.Models.db_9f8bee_konxdev.VwFreeTierDashboard>().HasNoKey();

            builder.Entity<KonXProWebApp.Models.db_9f8bee_konxdev.VwHighTierDashboard>().HasNoKey();

            builder.Entity<KonXProWebApp.Models.db_9f8bee_konxdev.VwMidTierDashboard>().HasNoKey();

            builder.Entity<KonXProWebApp.Models.db_9f8bee_konxdev.BlogContent>()
              .Property(p => p.CompletionDate)
              .HasColumnType("datetime2");

            builder.Entity<KonXProWebApp.Models.db_9f8bee_konxdev.DobViolation>()
              .Property(p => p.IssueDate)
              .HasColumnType("datetime");

            builder.Entity<KonXProWebApp.Models.db_9f8bee_konxdev.DobjobFiling>()
              .Property(p => p.LatestActionDate)
              .HasColumnType("datetime");

            builder.Entity<KonXProWebApp.Models.db_9f8bee_konxdev.DobjobFiling>()
              .Property(p => p.PreFilingDate)
              .HasColumnType("datetime");

            builder.Entity<KonXProWebApp.Models.db_9f8bee_konxdev.DobjobFiling>()
              .Property(p => p.Paid)
              .HasColumnType("datetime");

            builder.Entity<KonXProWebApp.Models.db_9f8bee_konxdev.DobjobFiling>()
              .Property(p => p.FullyPaid)
              .HasColumnType("datetime");

            builder.Entity<KonXProWebApp.Models.db_9f8bee_konxdev.DobjobFiling>()
              .Property(p => p.Assigned)
              .HasColumnType("datetime");

            builder.Entity<KonXProWebApp.Models.db_9f8bee_konxdev.DobjobFiling>()
              .Property(p => p.Approved)
              .HasColumnType("datetime");

            builder.Entity<KonXProWebApp.Models.db_9f8bee_konxdev.DobjobFiling>()
              .Property(p => p.FullyPermitted)
              .HasColumnType("datetime");

            builder.Entity<KonXProWebApp.Models.db_9f8bee_konxdev.DobjobFiling>()
              .Property(p => p.DobrunDate)
              .HasColumnType("datetime");

            builder.Entity<KonXProWebApp.Models.db_9f8bee_konxdev.DobjobFiling>()
              .Property(p => p.Signoffdate)
              .HasColumnType("datetime");

            builder.Entity<KonXProWebApp.Models.db_9f8bee_konxdev.VwBasicTierDashboard>()
              .Property(p => p.LatestActionDate)
              .HasColumnType("datetime");

            builder.Entity<KonXProWebApp.Models.db_9f8bee_konxdev.VwDemoDisplay>()
              .Property(p => p.CompletionDate)
              .HasColumnType("datetime2");

            builder.Entity<KonXProWebApp.Models.db_9f8bee_konxdev.VwFreeTierDashboard>()
              .Property(p => p.LatestActionDate)
              .HasColumnType("datetime");

            builder.Entity<KonXProWebApp.Models.db_9f8bee_konxdev.VwHighTierDashboard>()
              .Property(p => p.LatestActionDate)
              .HasColumnType("datetime");

            builder.Entity<KonXProWebApp.Models.db_9f8bee_konxdev.VwMidTierDashboard>()
              .Property(p => p.LatestActionDate)
              .HasColumnType("datetime");
            this.OnModelBuilding(builder);
        }

        public DbSet<KonXProWebApp.Models.db_9f8bee_konxdev.BlogContent> BlogContents { get; set; }

        public DbSet<KonXProWebApp.Models.db_9f8bee_konxdev.BlogFeedSource> BlogFeedSources { get; set; }

        public DbSet<KonXProWebApp.Models.db_9f8bee_konxdev.DobViolation> DobViolations { get; set; }

        public DbSet<KonXProWebApp.Models.db_9f8bee_konxdev.DobjobFiling> DobjobFilings { get; set; }

        public DbSet<KonXProWebApp.Models.db_9f8bee_konxdev.EcbViolation> EcbViolations { get; set; }

        public DbSet<KonXProWebApp.Models.db_9f8bee_konxdev.VwBasicTierDashboard> VwBasicTierDashboards { get; set; }

        public DbSet<KonXProWebApp.Models.db_9f8bee_konxdev.VwDemoDisplay> VwDemoDisplays { get; set; }

        public DbSet<KonXProWebApp.Models.db_9f8bee_konxdev.VwFreeTierDashboard> VwFreeTierDashboards { get; set; }

        public DbSet<KonXProWebApp.Models.db_9f8bee_konxdev.VwHighTierDashboard> VwHighTierDashboards { get; set; }

        public DbSet<KonXProWebApp.Models.db_9f8bee_konxdev.VwMidTierDashboard> VwMidTierDashboards { get; set; }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            configurationBuilder.Conventions.Add(_ => new BlankTriggerAddingConvention());
        }
    }
}
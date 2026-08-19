using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

using KonXProWebApp.Models;

namespace KonXProWebApp.Data
{
    public partial class ApplicationIdentityDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        public ApplicationIdentityDbContext(DbContextOptions<ApplicationIdentityDbContext> options) : base(options)
        {
        }

        public ApplicationIdentityDbContext()
        {
        }


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.ConfigureWarnings(warnings =>
                        warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        }
        partial void OnModelBuilding(ModelBuilder builder);

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ApplicationUser>()
                   .HasMany(u => u.Roles)
                   .WithMany(r => r.Users)
                   .UsingEntity<IdentityUserRole<string>>();


            builder.Entity<ApplicationUser>()
                .HasOne(i => i.ApplicationTenant)
                .WithMany(i => i.Users)
                .HasForeignKey(i => i.TenantId)
                .HasPrincipalKey(i => i.Id);

            builder.Entity<ApplicationRole>()
                .HasOne(i => i.ApplicationTenant)
                .WithMany(i => i.Roles)
                .HasForeignKey(i => i.TenantId)
                .HasPrincipalKey(i => i.Id);
            this.OnModelBuilding(builder);
        }

        public DbSet<ApplicationTenant> Tenants
        {
            get;
            set;
        }

        public async Task SeedTenantsAdmin()
        {
            var user = new ApplicationUser
            {
                UserName = "tenantsadmin",
                NormalizedUserName = "TENANTSADMIN",
                Email = "tenantsadmin",
                NormalizedEmail = "TENANTSADMIN",
                EmailConfirmed = true,
                LockoutEnabled = false,
                SecurityStamp = Guid.NewGuid().ToString()
            };

            if (!this.Users.Any(u => u.UserName == user.UserName))
            {
                var password = new Microsoft.AspNetCore.Identity.PasswordHasher<ApplicationUser>();
                var hashed = password.HashPassword(user, "tenantsadmin");
                user.PasswordHash = hashed;
                var userStore = new UserStore<ApplicationUser>(this);
                await userStore.CreateAsync(user);
            }

            await this.SaveChangesAsync();

            // Seed Admin and Marketing roles
            var adminRole = new ApplicationRole { Name = "Admin", NormalizedName = "ADMIN", Id = Guid.NewGuid().ToString() };
            var marketingRole = new ApplicationRole { Name = "Marketing", NormalizedName = "MARKETING", Id = Guid.NewGuid().ToString() };

            if (!this.Roles.Any(r => r.Name == "Admin"))
            {
                this.Roles.Add(adminRole);
            }
            if (!this.Roles.Any(r => r.Name == "Marketing"))
            {
                this.Roles.Add(marketingRole);
            }
            await this.SaveChangesAsync();

            // Assign tenantsadmin to Admin role
            var dbUser = this.Users.FirstOrDefault(u => u.UserName == "tenantsadmin");
            var dbRole = this.Roles.FirstOrDefault(r => r.Name == "Admin");
            if (dbUser != null && dbRole != null)
            {
                var hasRole = this.UserRoles.Any(ur => ur.UserId == dbUser.Id && ur.RoleId == dbRole.Id);
                if (!hasRole)
                {
                    this.UserRoles.Add(new IdentityUserRole<string> { UserId = dbUser.Id, RoleId = dbRole.Id });
                    await this.SaveChangesAsync();
                }
            }
        }

        public async Task SeedTierTestUsersAsync(KonXProWebApp.Data.db_9f8bee_konxdevContext permitDb)
        {
            var testTiers = new (string Email, string Tier)[]
            {
                ("starter_test@konxpro.com", "Starter"),
                ("pro_test@konxpro.com", "Pro"),
                ("business_test@konxpro.com", "Business"),
                ("agency_test@konxpro.com", "Agency")
            };

            var passwordHasher = new PasswordHasher<ApplicationUser>();

            foreach (var (email, tier) in testTiers)
            {
                var user = await this.Users.FirstOrDefaultAsync(u => u.UserName == email || u.Email == email);
                if (user == null)
                {
                    user = new ApplicationUser
                    {
                        Id = Guid.NewGuid().ToString(),
                        UserName = email,
                        NormalizedUserName = email.ToUpperInvariant(),
                        Email = email,
                        NormalizedEmail = email.ToUpperInvariant(),
                        EmailConfirmed = true,
                        LockoutEnabled = false,
                        SecurityStamp = Guid.NewGuid().ToString(),
                        ConcurrencyStamp = Guid.NewGuid().ToString()
                    };
                    user.PasswordHash = passwordHasher.HashPassword(user, "KonXTest2026!");
                    this.Users.Add(user);
                    await this.SaveChangesAsync();
                }
                else
                {
                    user.EmailConfirmed = true;
                    user.LockoutEnabled = false;
                    user.PasswordHash = passwordHasher.HashPassword(user, "KonXTest2026!");
                    await this.SaveChangesAsync();
                }

                if (permitDb != null)
                {
                    var sub = await permitDb.Subscriptions.FirstOrDefaultAsync(s => s.UserId == user.Id);
                    if (sub == null)
                    {
                        permitDb.Subscriptions.Add(new KonXProWebApp.Models.PermitIntel.Subscription
                        {
                            UserId = user.Id,
                            Tier = tier,
                            Status = "Active",
                            StartDate = DateTime.UtcNow,
                            EndDate = DateTime.UtcNow.AddYears(2),
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        });
                    }
                    else
                    {
                        sub.Tier = tier;
                        sub.Status = "Active";
                        sub.UpdatedAt = DateTime.UtcNow;
                    }
                }
            }

            if (permitDb != null)
            {
                await permitDb.SaveChangesAsync();
            }
        }
    }

    public class MultiTenancyUserStore : UserStore<ApplicationUser, ApplicationRole, ApplicationIdentityDbContext>
    {
        private readonly IHttpContextAccessor httpContextAccessor;
        public MultiTenancyUserStore(IHttpContextAccessor httpContextAccessor, ApplicationIdentityDbContext context, IdentityErrorDescriber describer = null) : base(context, describer)
        {
            this.httpContextAccessor = httpContextAccessor;
        }

        private ApplicationTenant GetTenant()
        {
            var tenants = Context.Set<ApplicationTenant>().ToList();

            var host = httpContextAccessor.HttpContext.Request.Host.Value;

            return tenants.Where(t => t.Hosts.Split(',').Where(h => h.Contains(host)).Any()).FirstOrDefault();
        }

        protected override async Task<ApplicationRole> FindRoleAsync(string normalizedRoleName, System.Threading.CancellationToken cancellationToken)
        {
            var tenant = GetTenant();
            ApplicationRole role = null;

            if (tenant != null)
            {
                role = await Context.Set<ApplicationRole>().SingleOrDefaultAsync(r => r.NormalizedName == normalizedRoleName && r.TenantId == tenant.Id, cancellationToken);
            }

            if (role == null)
            {
                role = await Context.Set<ApplicationRole>().FirstOrDefaultAsync(r => r.NormalizedName == normalizedRoleName, cancellationToken);
            }

            return role;
        }

        public override async Task<ApplicationUser> FindByNameAsync(string normalizedName, CancellationToken cancellationToken = default)
        {
            if (normalizedName.ToLower() == "tenantsadmin" || normalizedName.ToLower().EndsWith("_test@konxpro.com"))
            {
                return await base.FindByNameAsync(normalizedName, cancellationToken);
            }

            var tenant = GetTenant();
            ApplicationUser user = null;

            if (tenant != null)
            {
                user = await Context.Set<ApplicationUser>().SingleOrDefaultAsync(r => r.NormalizedUserName == normalizedName && r.TenantId == tenant.Id, cancellationToken);
            }

            if (user == null)
            {
                user = await Context.Set<ApplicationUser>().FirstOrDefaultAsync(r => r.NormalizedUserName == normalizedName, cancellationToken);
            }

            return user;
        }

        public override async Task AddToRoleAsync(ApplicationUser user, string normalizedRoleName, CancellationToken cancellationToken = default)
        {
            if (user.NormalizedUserName.ToLower() == "tenantsadmin")
            {
                await base.AddToRoleAsync(user, normalizedRoleName, cancellationToken);
            }

            var tenant = user.ApplicationTenant ?? GetTenant();
            ApplicationRole role = null;

            if (tenant != null)
            {
                role = await Context.Set<ApplicationRole>().SingleOrDefaultAsync(r => r.NormalizedName == normalizedRoleName && r.TenantId == tenant.Id, cancellationToken);
            }

            if (role != null)
            {
                Context.Set<IdentityUserRole<string>>().Add(new IdentityUserRole<string>
                {
                    RoleId = role.Id,
                    UserId = user.Id
                });
            }

            await Context.SaveChangesAsync();
        }
    }
}
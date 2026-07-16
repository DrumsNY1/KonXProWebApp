using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using KonXProWebApp.Data;
using KonXProWebApp.Models;
using KonXProWebApp.Models.db_9f8bee_konxdev;
using KonXProWebApp.Models.PermitIntel;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using Radzen;
using Radzen.Blazor;

namespace KonXProWebApp.Components.Pages
{
    public partial class HomeImprovementContractors
    {
        [Inject]
        protected db_9f8bee_konxdevContext DbContext { get; set; }

        [Inject]
        protected ApplicationIdentityDbContext IdentityDbContext { get; set; }

        [Inject]
        protected SecurityService Security { get; set; }

        [Inject]
        protected DialogService DialogService { get; set; }

        [Inject]
        protected NotificationService NotificationService { get; set; }

        [Inject]
        protected NavigationManager NavigationManager { get; set; }

        [Inject]
        protected HttpClient HttpClient { get; set; }

        [Inject]
        protected IJSRuntime JSRuntime { get; set; }

        private IEnumerable<HomeImprovementContractor> contractors = new List<HomeImprovementContractor>();
        private RadzenDataGrid<HomeImprovementContractor> grid;
        
        private bool isLoading = true;
        private bool isSyncing = false;

        // Statistics
        private int totalCount = 0;
        private int activeCount = 0;
        private int postcardsCount = 0;
        private int emailsCount = 0;

        // A/B Campaign Metrics
        private int cohortAReach = 0;
        private int cohortAClicks = 0;
        private int cohortAConversions = 0;

        private int cohortBReach = 0;
        private int cohortBClicks = 0;
        private int cohortBConversions = 0;

        private int cohortCReach = 0;
        private int cohortCClicks = 0;
        private int cohortCConversions = 0;

        private int GetPercent(int part, int total)
        {
            if (total <= 0) return 0;
            return (int)Math.Round((double)part * 100 / total);
        }

        protected override async Task OnInitializedAsync()
        {
            await LoadData();
        }

        private async Task LoadData()
        {
            isLoading = true;
            try
            {
                contractors = await DbContext.HomeImprovementContractors
                    .AsNoTracking()
                    .OrderByDescending(c => c.IngestedAt)
                    .ToListAsync();

                totalCount = contractors.Count();
                activeCount = contractors.Count(c => c.LicenseStatus == "Active");
                postcardsCount = contractors.Count(c => c.PostcardSent);
                emailsCount = contractors.Count(c => c.EmailSent);

                // Compute Campaign Cohort Statistics
                cohortAReach = contractors.Count(c => c.CampaignCohort == "A" && (c.PostcardSent || c.EmailSent));
                cohortAClicks = contractors.Count(c => c.CampaignCohort == "A" && c.CampaignVisited);
                cohortAConversions = contractors.Count(c => c.CampaignCohort == "A" && c.CampaignConverted);

                cohortBReach = contractors.Count(c => c.CampaignCohort == "B" && (c.PostcardSent || c.EmailSent));
                cohortBClicks = contractors.Count(c => c.CampaignCohort == "B" && c.CampaignVisited);
                cohortBConversions = contractors.Count(c => c.CampaignCohort == "B" && c.CampaignConverted);

                cohortCReach = contractors.Count(c => c.CampaignCohort == "C" && (c.PostcardSent || c.EmailSent));
                cohortCClicks = contractors.Count(c => c.CampaignCohort == "C" && c.CampaignVisited);
                cohortCConversions = contractors.Count(c => c.CampaignCohort == "C" && c.CampaignConverted);
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error, "Error", $"Failed to load contractors: {ex.Message}");
            }
            finally
            {
                isLoading = false;
            }
        }

        private async Task SyncFromSocrata()
        {
            if (isSyncing) return;

            try
            {
                isSyncing = true;
                StateHasChanged();

                // Call Socrata NYC Open Data directly for DCA Home Improvement Contractors
                var url = "https://data.cityofnewyork.us/resource/w7w3-xahh.json?business_category=Home%20Improvement%20Contractor&$limit=200";
                var records = await HttpClient.GetFromJsonAsync<List<SocrataContractorRecord>>(url);

                if (records == null || !records.Any())
                {
                    NotificationService.Notify(NotificationSeverity.Warning, "Sync", "No contractors found on NYC Open Data.");
                    return;
                }

                int inserted = 0;
                int updated = 0;

                foreach (var r in records)
                {
                    if (string.IsNullOrEmpty(r.license_nbr)) continue;

                    var existing = await DbContext.HomeImprovementContractors
                        .FirstOrDefaultAsync(c => c.LicenseNumber == r.license_nbr);

                    var expDate = DateTime.TryParse(r.license_expiration_date, out var parsedExp) ? (DateTime?)parsedExp : null;
                    var issueDate = DateTime.TryParse(r.license_creation_date, out var parsedIssue) ? (DateTime?)parsedIssue : null;

                    if (existing == null)
                    {
                        var newContractor = new HomeImprovementContractor
                        {
                            LicenseNumber = r.license_nbr,
                            BusinessName = r.business_name ?? r.dba_trade_name ?? "Unknown Contractor",
                            DbaTradeName = r.dba_trade_name,
                            BusinessUniqueId = r.business_unique_id ?? Guid.NewGuid().ToString().Substring(0, 8),
                            LicenseStatus = r.license_status ?? "Active",
                            LicenseIssueDate = issueDate,
                            LicenseExpirationDate = expDate,
                            ContactPhoneNumber = r.contact_phone_number,
                            AddressBuilding = r.address_building,
                            AddressStreetName = r.address_street_name,
                            AddressCity = r.address_city,
                            AddressState = r.address_state,
                            AddressZip = r.address_zip,
                            Borough = r.address_borough,
                            IngestedAt = DateTime.UtcNow,
                            SalesStatus = "New"
                        };
                        DbContext.HomeImprovementContractors.Add(newContractor);
                        inserted++;
                    }
                    else
                    {
                        existing.BusinessName = r.business_name ?? r.dba_trade_name ?? existing.BusinessName;
                        existing.DbaTradeName = r.dba_trade_name ?? existing.DbaTradeName;
                        existing.LicenseStatus = r.license_status ?? existing.LicenseStatus;
                        existing.LicenseExpirationDate = expDate ?? existing.LicenseExpirationDate;
                        existing.ContactPhoneNumber = r.contact_phone_number ?? existing.ContactPhoneNumber;
                        existing.AddressBuilding = r.address_building ?? existing.AddressBuilding;
                        existing.AddressStreetName = r.address_street_name ?? existing.AddressStreetName;
                        existing.AddressCity = r.address_city ?? existing.AddressCity;
                        existing.AddressState = r.address_state ?? existing.AddressState;
                        existing.AddressZip = r.address_zip ?? existing.AddressZip;
                        existing.Borough = r.address_borough ?? existing.Borough;
                        updated++;
                    }
                }

                await DbContext.SaveChangesAsync();
                NotificationService.Notify(NotificationSeverity.Success, "Sync Successful", $"Successfully synced with NYC Open Data (+{inserted} new, ~{updated} updated).");
                await LoadData();
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error, "Sync Failed", ex.Message);
            }
            finally
            {
                isSyncing = false;
                StateHasChanged();
            }
        }

        private async Task ExportCsvForPostcards()
        {
            if (contractors == null || !contractors.Any())
            {
                NotificationService.Notify(NotificationSeverity.Warning, "Export", "No contractors to export.");
                return;
            }

            try
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("Recipient Name,Address Line 1,Address Line 2,City,State,Zip Code,Cohort,Tracking URL");

                foreach (var c in contractors)
                {
                    var name = EscapeCsv(c.BusinessName);
                    var addr1 = EscapeCsv((c.AddressBuilding ?? "") + " " + (c.AddressStreetName ?? "")).Trim();
                    var addr2 = "";
                    var city = EscapeCsv(c.AddressCity ?? c.Borough ?? "New York");
                    var state = EscapeCsv(c.AddressState ?? "NY");
                    var zip = EscapeCsv(c.AddressZip);

                    var cohort = c.CampaignCohort ?? "A";
                    var path = cohort == "C" ? "territory" : cohort == "B" ? "leads" : "first";
                    var trackingUrl = $"https://konxpro.com/{path}?cid={c.Id}";

                    sb.AppendLine($"{name},{addr1},{addr2},{city},{state},{zip},{cohort},{trackingUrl}");
                }

                var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
                var base64 = Convert.ToBase64String(bytes);
                var filename = $"postcard_mailing_list_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

                await JSRuntime.InvokeVoidAsync("saveAsFile", filename, base64);
                NotificationService.Notify(NotificationSeverity.Success, "Export Successful", $"Mailing list exported ({contractors.Count()} recipients).");
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error, "Export Failed", ex.Message);
            }
        }

        private string EscapeCsv(string val)
        {
            if (string.IsNullOrEmpty(val)) return "";
            if (val.Contains(",") || val.Contains("\"") || val.Contains("\n"))
            {
                return $"\"{val.Replace("\"", "\"\"")}\"";
            }
            return val;
        }

        private async Task UpdateSalesStatus(HomeImprovementContractor contractor, string newStatus)
        {
            try
            {
                var entry = await DbContext.HomeImprovementContractors.FindAsync(contractor.Id);
                if (entry != null)
                {
                    entry.SalesStatus = newStatus;
                    await DbContext.SaveChangesAsync();
                    contractor.SalesStatus = newStatus;
                    NotificationService.Notify(NotificationSeverity.Success, "Status Updated", $"Status set to '{newStatus}' for {contractor.BusinessName}.");
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error, "Error", ex.Message);
            }
        }

        private async Task UpdateEmailAddress(HomeImprovementContractor contractor, string email)
        {
            try
            {
                var entry = await DbContext.HomeImprovementContractors.FindAsync(contractor.Id);
                if (entry != null)
                {
                    entry.EmailAddress = email;
                    await DbContext.SaveChangesAsync();
                    contractor.EmailAddress = email;
                    NotificationService.Notify(NotificationSeverity.Success, "Email Updated", $"Email updated for {contractor.BusinessName}.");
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error, "Error", ex.Message);
            }
        }

        private async Task TogglePostcardSent(HomeImprovementContractor contractor)
        {
            try
            {
                var entry = await DbContext.HomeImprovementContractors.FindAsync(contractor.Id);
                if (entry != null)
                {
                    entry.PostcardSent = !entry.PostcardSent;
                    await DbContext.SaveChangesAsync();
                    contractor.PostcardSent = entry.PostcardSent;
                    postcardsCount = contractors.Count(c => c.PostcardSent);
                    NotificationService.Notify(NotificationSeverity.Success, "Tracking Updated", $"Postcard state updated.");
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error, "Error", ex.Message);
            }
        }

        private async Task ToggleEmailSent(HomeImprovementContractor contractor)
        {
            try
            {
                var entry = await DbContext.HomeImprovementContractors.FindAsync(contractor.Id);
                if (entry != null)
                {
                    entry.EmailSent = !entry.EmailSent;
                    await DbContext.SaveChangesAsync();
                    contractor.EmailSent = entry.EmailSent;
                    emailsCount = contractors.Count(c => c.EmailSent);
                    NotificationService.Notify(NotificationSeverity.Success, "Tracking Updated", $"Email outreach state updated.");
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error, "Error", ex.Message);
            }
        }

        private async Task EditNotes(HomeImprovementContractor contractor)
        {
            var result = await DialogService.OpenAsync<NotesEditDialog>($"Edit Notes - {contractor.BusinessName}",
                new Dictionary<string, object> { { "Notes", contractor.SalesNotes ?? "" } },
                new DialogOptions { Width = "500px", Height = "300px" });

            if (result is string updatedNotes)
            {
                try
                {
                    var entry = await DbContext.HomeImprovementContractors.FindAsync(contractor.Id);
                    if (entry != null)
                    {
                        entry.SalesNotes = updatedNotes;
                        await DbContext.SaveChangesAsync();
                        contractor.SalesNotes = updatedNotes;
                        NotificationService.Notify(NotificationSeverity.Success, "Notes Updated", "Sales outreach notes saved.");
                    }
                }
                catch (Exception ex)
                {
                    NotificationService.Notify(NotificationSeverity.Error, "Error", ex.Message);
                }
            }
        }

        private async Task ConvertToCustomer(HomeImprovementContractor contractor)
        {
            var result = await DialogService.OpenAsync<OnboardCustomerDialog>($"Onboard {contractor.BusinessName}",
                new Dictionary<string, object> { { "DefaultEmail", contractor.EmailAddress ?? "" } },
                new DialogOptions { Width = "550px" });

            if (result is OnboardModel onboardDetails)
            {
                try
                {
                    // 1. Create the new ApplicationUser
                    await Security.Register(onboardDetails.Email, onboardDetails.Password);

                    // 2. Resolve the new user to get their user ID
                    var newUser = await IdentityDbContext.Users.FirstOrDefaultAsync(u => u.UserName == onboardDetails.Email);
                    if (newUser != null)
                    {
                        // 3. Add an active subscription for the selected tier
                        var subscription = new KonXProWebApp.Models.PermitIntel.Subscription
                        {
                            UserId = newUser.Id,
                            Tier = onboardDetails.Tier,
                            Status = "Active",
                            StartDate = DateTime.UtcNow,
                            TrialEndDate = DateTime.UtcNow.AddDays(7),
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        DbContext.Subscriptions.Add(subscription);

                        // 4. Update the contractor's status
                        var entry = await DbContext.HomeImprovementContractors.FindAsync(contractor.Id);
                        if (entry != null)
                        {
                            entry.SalesStatus = "Subscribed";
                            entry.EmailAddress = onboardDetails.Email;
                            await DbContext.SaveChangesAsync();
                            contractor.SalesStatus = "Subscribed";
                            contractor.EmailAddress = onboardDetails.Email;
                        }

                        NotificationService.Notify(NotificationSeverity.Success, "Onboarding Successful", 
                            $"{contractor.BusinessName} is now a KonXPro active {onboardDetails.Tier} customer!");
                        await LoadData();
                    }
                }
                catch (Exception ex)
                {
                    NotificationService.Notify(NotificationSeverity.Error, "Onboarding Failed", ex.Message);
                }
            }
        }

        private class SocrataContractorRecord
        {
            public string license_nbr { get; set; }
            public string business_name { get; set; }
            public string dba_trade_name { get; set; }
            public string business_unique_id { get; set; }
            public string license_status { get; set; }
            public string license_creation_date { get; set; }
            public string license_expiration_date { get; set; }
            public string contact_phone_number { get; set; }
            public string address_building { get; set; }
            public string address_street_name { get; set; }
            public string address_city { get; set; }
            public string address_state { get; set; }
            public string address_zip { get; set; }
            public string address_borough { get; set; }
        }

        private async Task AutoAssignCohorts()
        {
            try
            {
                var unassigned = await DbContext.HomeImprovementContractors
                    .Where(c => string.IsNullOrEmpty(c.CampaignCohort))
                    .ToListAsync();

                if (!unassigned.Any())
                {
                    NotificationService.Notify(NotificationSeverity.Info, "Cohort Assignment", "All contractors already have assigned cohorts.");
                    return;
                }

                string[] cohorts = { "A", "B", "C" };
                for (int i = 0; i < unassigned.Count; i++)
                {
                    unassigned[i].CampaignCohort = cohorts[i % 3];
                }

                await DbContext.SaveChangesAsync();
                NotificationService.Notify(NotificationSeverity.Success, "Cohort Assignment", $"Assigned A/B/C cohorts to {unassigned.Count} contractors.");
                await LoadData();
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error, "Error", $"Failed to assign cohorts: {ex.Message}");
            }
        }

        public class OnboardModel
        {
            public string Email { get; set; }
            public string Password { get; set; }
            public string Tier { get; set; }
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KonXProWebApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceRequests311 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.CreateTable(
                name: "AlertPreferences",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Boroughs = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    JobTypes = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Trades = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MinCost = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MaxCost = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AlertChannel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AlertFrequency = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertPreferences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BlogContent",
                schema: "dbo",
                columns: table => new
                {
                    ContentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Summary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompletionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SourceID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlogContent", x => x.ContentID);
                });

            migrationBuilder.CreateTable(
                name: "BlogFeedSources",
                schema: "dbo",
                columns: table => new
                {
                    FeedID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FeedName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FeedUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FeedCategory = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlogFeedSources", x => x.FeedID);
                });

            migrationBuilder.CreateTable(
                name: "DOB_Violations",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    isn_dob_bis_viol = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    boro = table.Column<int>(type: "int", nullable: false),
                    bin = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    block = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    lot = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    issue_date = table.Column<DateTime>(type: "datetime", nullable: false),
                    violation_type_code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    violation_number = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    house_number = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    street = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    number = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    violation_category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    violation_type = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DOB_Violations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "DOBJobFilings",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobNum = table.Column<int>(type: "int", nullable: true),
                    DocNum = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Borough = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HouseNum = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StreetName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Block = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Lot = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Bin = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JobType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JobStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JobStatusDescrp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LatestActionDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    BuildingType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CommunityBoard = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Cluster = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Landmarked = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AdultEstab = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LoftBoard = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CityOwned = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Littlee = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PCFiled = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    eFilingFiled = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Plumbing = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Mechanical = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Boiler = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FuelBurning = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FuelStorage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Standpipe = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Sprinkler = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FireAlarm = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Equipment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FireSuppression = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CurbCut = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Other = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OtherDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApplicantsFirstName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApplicantsLastName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApplicantProfessionalTitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApplicantLicenseNum = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProfessionalCert = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PreFilingDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    Paid = table.Column<DateTime>(type: "datetime", nullable: true),
                    FullyPaid = table.Column<DateTime>(type: "datetime", nullable: true),
                    Assigned = table.Column<DateTime>(type: "datetime", nullable: true),
                    Approved = table.Column<DateTime>(type: "datetime", nullable: true),
                    FullyPermitted = table.Column<DateTime>(type: "datetime", nullable: true),
                    InitialCost = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TotalEstFee = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    FeeStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExistingZoningSqft = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProposedZoningSqft = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HorizontalEnlrgmt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VerticalEnlrgmt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EnlargementSQFootage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StreetFrontage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExistingNoofStories = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProposedNoofStories = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExistingHeight = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProposedHeight = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExistingDwellingUnits = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProposedDwellingUnits = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExistingOccupancy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProposedOccupancy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SiteFill = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ZoningDist1 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ZoningDist2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ZoningDist3 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SpecialDistrict1 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SpecialDistrict2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OwnerType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NonProfit = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OwnersFirstName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OwnersLastName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OwnersBusinessName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OwnersHouseNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OwnersHouseStreetName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    State = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Zip = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OwnersPhone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JobDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DOBRunDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    JOBS1NO = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TOTALCONSTRUCTIONFLOORAREA = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WITHDRAWALFLAG = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SIGNOFFDATE = table.Column<DateTime>(type: "datetime", nullable: true),
                    SPECIALACTIONSTATUS = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SPECIALACTIONDATE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BUILDINGCLASS = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JOBNOGOODCOUNT = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GISLATITUDE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GISLONGITUDE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GISCOUNCILDISTRICT = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GISCENSUSTRACT = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GISNTANAME = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GISBIN = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LeadScore = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DOBJobFilings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ECB_Violations",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    isn_dob_bis_extract = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ecb_violation_number = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ecb_violation_status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    bin = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    boro = table.Column<int>(type: "int", nullable: false),
                    block = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    lot = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    hearing_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    hearing_time = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    served_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    issue_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    severity = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    violation_type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    respondent_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    respondent_house_number = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    respondent_street = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    respondent_city = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    respondent_zip = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    violation_description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    penality_imposed = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    amount_paid = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    balance_due = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    infraction_code1 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    section_law_description1 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    aggravated_level = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    hearing_status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    certification_status = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ECB_Violations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "HPD_Violations",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    violation_id = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    building_id = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    boro = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    boro_id = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    bin = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    block = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    lot = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    house_number = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    street_name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    zip = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    apartment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    inspection_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    approved_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    original_certify_by_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    original_correct_by_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    violation_status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    violation_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    nov_description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    @class = table.Column<string>(name: "class", type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HPD_Violations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "IngestionLogs",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RunDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RecordsIngested = table.Column<int>(type: "int", nullable: false),
                    RecordsUpdated = table.Column<int>(type: "int", nullable: false),
                    RecordsSkipped = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastSocrataTimestamp = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IngestionLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServiceRequests311",
                schema: "dbo",
                columns: table => new
                {
                    UniqueKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Agency = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ComplaintType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Descriptor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IncidentZip = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IncidentAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Borough = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Bbl = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceRequests311", x => x.UniqueKey);
                });

            migrationBuilder.CreateTable(
                name: "Subscriptions",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    StripeCustomerId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StripeSubscriptionId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Tier = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TrialEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subscriptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "vwBasicTierDashboard",
                schema: "dbo",
                columns: table => new
                {
                    JobNum = table.Column<int>(type: "int", nullable: true),
                    Borough = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HouseNum = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Street = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LatestActionDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    ProjectType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JobDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Neighborhood = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "vwDemoDisplay",
                schema: "dbo",
                columns: table => new
                {
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Summary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompletionDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "vwFreeTierDashboard",
                schema: "dbo",
                columns: table => new
                {
                    JobNum = table.Column<int>(type: "int", nullable: true),
                    Borough = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Street = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LatestActionDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    ProjectType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JobDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Neighborhood = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "vwHighTierDashboard",
                schema: "dbo",
                columns: table => new
                {
                    JobNum = table.Column<int>(type: "int", nullable: true),
                    Borough = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HouseNum = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Street = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LatestActionDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    ProjectType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EstimatedCost = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    JobDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Neighborhood = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "vwMidTierDashboard",
                schema: "dbo",
                columns: table => new
                {
                    JobNum = table.Column<int>(type: "int", nullable: true),
                    Borough = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HouseNum = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Street = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LatestActionDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    ProjectType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EstimatedCost = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    JobDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Neighborhood = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "SavedLeads",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DobjobFilingId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SavedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedLeads", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SavedLeads_DOBJobFilings_DobjobFilingId",
                        column: x => x.DobjobFilingId,
                        principalSchema: "dbo",
                        principalTable: "DOBJobFilings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlertPreferences_UserId",
                schema: "dbo",
                table: "AlertPreferences",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedLeads_DobjobFilingId",
                schema: "dbo",
                table: "SavedLeads",
                column: "DobjobFilingId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedLeads_UserId",
                schema: "dbo",
                table: "SavedLeads",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedLeads_UserId_DobjobFilingId",
                schema: "dbo",
                table: "SavedLeads",
                columns: new[] { "UserId", "DobjobFilingId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequests311_Bbl",
                schema: "dbo",
                table: "ServiceRequests311",
                column: "Bbl");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_StripeSubscriptionId",
                schema: "dbo",
                table: "Subscriptions",
                column: "StripeSubscriptionId",
                unique: true,
                filter: "[StripeSubscriptionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_UserId",
                schema: "dbo",
                table: "Subscriptions",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlertPreferences",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "BlogContent",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "BlogFeedSources",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "DOB_Violations",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ECB_Violations",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "HPD_Violations",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "IngestionLogs",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "SavedLeads",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ServiceRequests311",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Subscriptions",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "vwBasicTierDashboard",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "vwDemoDisplay",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "vwFreeTierDashboard",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "vwHighTierDashboard",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "vwMidTierDashboard",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "DOBJobFilings",
                schema: "dbo");
        }
    }
}

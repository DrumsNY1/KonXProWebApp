using KonXProWebApp.Functions.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Globalization;

namespace KonXProWebApp.Functions.Services;

/// <summary>
/// Handles mapping Socrata API records to the DOBJobFilings table
/// and performing efficient upsert operations.
/// </summary>
public class IngestionService
{
    private readonly string _connectionString;
    private readonly ILogger<IngestionService> _logger;

    public IngestionService(IConfiguration configuration, ILogger<IngestionService> logger)
    {
        _connectionString = configuration["SqlConnectionString"]
            ?? throw new InvalidOperationException("SqlConnectionString not configured");
        _logger = logger;
    }

    /// <summary>
    /// Upserts a batch of Socrata records into DOBJobFilings.
    /// Returns (inserted, updated, skipped) counts.
    /// </summary>
    public async Task<(int Inserted, int Updated, int Skipped)> UpsertPermits(
        IReadOnlyList<SocrataPermitRecord> records)
    {
        int inserted = 0, updated = 0, skipped = 0;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        foreach (var record in records)
        {
            try
            {
                var jobNum = ParseInt(record.JobNumber);
                if (!jobNum.HasValue)
                {
                    skipped++;
                    continue;
                }

                var result = await UpsertSinglePermit(connection, record, jobNum.Value);
                if (result == UpsertResult.Inserted) inserted++;
                else if (result == UpsertResult.Updated) updated++;
                else skipped++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to upsert record JobNum={JobNum}, DocNum={DocNum}",
                    record.JobNumber, record.DocNumber);
                skipped++;
            }
        }

        return (inserted, updated, skipped);
    }

    private async Task<UpsertResult> UpsertSinglePermit(
        SqlConnection connection, SocrataPermitRecord record, int jobNum)
    {
        // Use MERGE for atomic upsert
        const string mergeSql = @"
            MERGE DOBJobFilings AS target
            USING (SELECT @JobNum AS JobNum, @DocNum AS DocNum) AS source
            ON target.JobNum = source.JobNum AND target.DocNum = source.DocNum
            WHEN MATCHED THEN UPDATE SET
                Borough = @Borough,
                HouseNum = @HouseNum,
                StreetName = @StreetName,
                Block = @Block,
                Lot = @Lot,
                Bin = @Bin,
                JobType = @JobType,
                JobStatus = @JobStatus,
                JobStatusDescrp = @JobStatusDescrp,
                LatestActionDate = @LatestActionDate,
                BuildingType = @BuildingType,
                CommunityBoard = @CommunityBoard,
                Cluster = @Cluster,
                Landmarked = @Landmarked,
                AdultEstab = @AdultEstab,
                LoftBoard = @LoftBoard,
                CityOwned = @CityOwned,
                Littlee = @LittleE,
                PCFiled = @PcFiled,
                eFilingFiled = @EFilingFiled,
                Plumbing = @Plumbing,
                Mechanical = @Mechanical,
                Boiler = @Boiler,
                FuelBurning = @FuelBurning,
                FuelStorage = @FuelStorage,
                Standpipe = @Standpipe,
                Sprinkler = @Sprinkler,
                FireAlarm = @FireAlarm,
                Equipment = @Equipment,
                FireSuppression = @FireSuppression,
                CurbCut = @CurbCut,
                Other = @Other,
                OtherDescription = @OtherDescription,
                ApplicantsFirstName = @ApplicantFirstName,
                ApplicantsLastName = @ApplicantLastName,
                ApplicantProfessionalTitle = @ApplicantProfessionalTitle,
                ApplicantLicenseNum = @ApplicantLicenseNum,
                ProfessionalCert = @ProfessionalCert,
                PreFilingDate = @PreFilingDate,
                Paid = @Paid,
                FullyPaid = @FullyPaid,
                Assigned = @Assigned,
                Approved = @Approved,
                FullyPermitted = @FullyPermitted,
                InitialCost = @InitialCost,
                TotalEstFee = @TotalEstFee,
                FeeStatus = @FeeStatus,
                ExistingZoningSqft = @ExistingZoningSqft,
                ProposedZoningSqft = @ProposedZoningSqft,
                HorizontalEnlrgmt = @HorizontalEnlrgmt,
                VerticalEnlrgmt = @VerticalEnlrgmt,
                EnlargementSQFootage = @EnlargementSqFootage,
                StreetFrontage = @StreetFrontage,
                ExistingNoofStories = @ExistingNoOfStories,
                ProposedNoofStories = @ProposedNoOfStories,
                ExistingHeight = @ExistingHeight,
                ProposedHeight = @ProposedHeight,
                ExistingDwellingUnits = @ExistingDwellingUnits,
                ProposedDwellingUnits = @ProposedDwellingUnits,
                ExistingOccupancy = @ExistingOccupancy,
                ProposedOccupancy = @ProposedOccupancy,
                SiteFill = @SiteFill,
                ZoningDist1 = @ZoningDist1,
                ZoningDist2 = @ZoningDist2,
                ZoningDist3 = @ZoningDist3,
                SpecialDistrict1 = @SpecialDistrict1,
                SpecialDistrict2 = @SpecialDistrict2,
                OwnerType = @OwnerType,
                NonProfit = @NonProfit,
                OwnersFirstName = @OwnerFirstName,
                OwnersLastName = @OwnerLastName,
                OwnersBusinessName = @OwnerBusinessName,
                OwnersHouseNumber = @OwnerHouseNumber,
                OwnersHouseStreetName = @OwnerHouseStreetName,
                City = @City,
                State = @State,
                Zip = @Zip,
                OwnersPhone = @OwnerPhone,
                JobDescription = @JobDescription,
                DOBRunDate = @DobRunDate,
                JOBS1NO = @JobS1No,
                TOTALCONSTRUCTIONFLOORAREA = @TotalConstructionFloorArea,
                WITHDRAWALFLAG = @WithdrawalFlag,
                SIGNOFFDATE = @SignoffDate,
                SPECIALACTIONSTATUS = @SpecialActionStatus,
                SPECIALACTIONDATE = @SpecialActionDate,
                BUILDINGCLASS = @BuildingClass,
                JOBNOGOODCOUNT = @JobNoGoodCount,
                GISLATITUDE = @GisLatitude,
                GISLONGITUDE = @GisLongitude,
                GISCOUNCILDISTRICT = @GisCouncilDistrict,
                GISCENSUSTRACT = @GisCensusTract,
                GISNTANAME = @GisNtaName,
                GISBIN = @GisBin,
                LeadScore = @LeadScore
            WHEN NOT MATCHED THEN INSERT (
                JobNum, DocNum, Borough, HouseNum, StreetName, Block, Lot, Bin,
                JobType, JobStatus, JobStatusDescrp, LatestActionDate, BuildingType,
                CommunityBoard, Cluster, Landmarked, AdultEstab, LoftBoard, CityOwned,
                Littlee, PCFiled, eFilingFiled, Plumbing, Mechanical, Boiler,
                FuelBurning, FuelStorage, Standpipe, Sprinkler, FireAlarm, Equipment,
                FireSuppression, CurbCut, Other, OtherDescription,
                ApplicantsFirstName, ApplicantsLastName, ApplicantProfessionalTitle,
                ApplicantLicenseNum, ProfessionalCert, PreFilingDate, Paid, FullyPaid,
                Assigned, Approved, FullyPermitted, InitialCost, TotalEstFee, FeeStatus,
                ExistingZoningSqft, ProposedZoningSqft, HorizontalEnlrgmt, VerticalEnlrgmt,
                EnlargementSQFootage, StreetFrontage, ExistingNoofStories, ProposedNoofStories,
                ExistingHeight, ProposedHeight, ExistingDwellingUnits, ProposedDwellingUnits,
                ExistingOccupancy, ProposedOccupancy, SiteFill, ZoningDist1, ZoningDist2,
                ZoningDist3, SpecialDistrict1, SpecialDistrict2, OwnerType, NonProfit,
                OwnersFirstName, OwnersLastName, OwnersBusinessName, OwnersHouseNumber,
                OwnersHouseStreetName, City, State, Zip, OwnersPhone, JobDescription,
                DOBRunDate, JOBS1NO, TOTALCONSTRUCTIONFLOORAREA, WITHDRAWALFLAG, SIGNOFFDATE,
                SPECIALACTIONSTATUS, SPECIALACTIONDATE, BUILDINGCLASS, JOBNOGOODCOUNT,
                GISLATITUDE, GISLONGITUDE, GISCOUNCILDISTRICT, GISCENSUSTRACT, GISNTANAME,
                GISBIN, LeadScore
            ) VALUES (
                @JobNum, @DocNum, @Borough, @HouseNum, @StreetName, @Block, @Lot, @Bin,
                @JobType, @JobStatus, @JobStatusDescrp, @LatestActionDate, @BuildingType,
                @CommunityBoard, @Cluster, @Landmarked, @AdultEstab, @LoftBoard, @CityOwned,
                @LittleE, @PcFiled, @EFilingFiled, @Plumbing, @Mechanical, @Boiler,
                @FuelBurning, @FuelStorage, @Standpipe, @Sprinkler, @FireAlarm, @Equipment,
                @FireSuppression, @CurbCut, @Other, @OtherDescription,
                @ApplicantFirstName, @ApplicantLastName, @ApplicantProfessionalTitle,
                @ApplicantLicenseNum, @ProfessionalCert, @PreFilingDate, @Paid, @FullyPaid,
                @Assigned, @Approved, @FullyPermitted, @InitialCost, @TotalEstFee, @FeeStatus,
                @ExistingZoningSqft, @ProposedZoningSqft, @HorizontalEnlrgmt, @VerticalEnlrgmt,
                @EnlargementSqFootage, @StreetFrontage, @ExistingNoOfStories, @ProposedNoOfStories,
                @ExistingHeight, @ProposedHeight, @ExistingDwellingUnits, @ProposedDwellingUnits,
                @ExistingOccupancy, @ProposedOccupancy, @SiteFill, @ZoningDist1, @ZoningDist2,
                @ZoningDist3, @SpecialDistrict1, @SpecialDistrict2, @OwnerType, @NonProfit,
                @OwnerFirstName, @OwnerLastName, @OwnerBusinessName, @OwnerHouseNumber,
                @OwnerHouseStreetName, @City, @State, @Zip, @OwnerPhone, @JobDescription,
                @DobRunDate, @JobS1No, @TotalConstructionFloorArea, @WithdrawalFlag, @SignoffDate,
                @SpecialActionStatus, @SpecialActionDate, @BuildingClass, @JobNoGoodCount,
                @GisLatitude, @GisLongitude, @GisCouncilDistrict, @GisCensusTract, @GisNtaName,
                @GisBin, @LeadScore
            )
            OUTPUT $action;";

        await using var cmd = new SqlCommand(mergeSql, connection);
        AddParameters(cmd, record, jobNum);

        var action = (string)await cmd.ExecuteScalarAsync();
        return action switch
        {
            "INSERT" => UpsertResult.Inserted,
            "UPDATE" => UpsertResult.Updated,
            _ => UpsertResult.Skipped
        };
    }

    private void AddParameters(SqlCommand cmd, SocrataPermitRecord r, int jobNum)
    {
        cmd.Parameters.AddWithValue("@JobNum", jobNum);
        cmd.Parameters.AddWithValue("@DocNum", (object)r.DocNumber ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Borough", (object)r.Borough ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@HouseNum", (object)r.HouseNumber ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@StreetName", (object)r.StreetName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Block", (object)r.Block ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Lot", (object)r.Lot ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Bin", (object)r.Bin ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@JobType", (object)r.JobType ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@JobStatus", (object)r.JobStatus ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@JobStatusDescrp", (object)r.JobStatusDescription ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@LatestActionDate", (object)ParseDate(r.LatestActionDate) ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@BuildingType", (object)r.BuildingType ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@CommunityBoard", (object)r.CommunityBoard ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Cluster", (object)r.Cluster ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Landmarked", (object)r.Landmarked ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@AdultEstab", (object)r.AdultEstab ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@LoftBoard", (object)r.LoftBoard ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@CityOwned", (object)r.CityOwned ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@LittleE", (object)r.LittleE ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@PcFiled", (object)r.PcFiled ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@EFilingFiled", (object)r.EFilingFiled ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Plumbing", (object)r.Plumbing ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Mechanical", (object)r.Mechanical ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Boiler", (object)r.Boiler ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@FuelBurning", (object)r.FuelBurning ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@FuelStorage", (object)r.FuelStorage ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Standpipe", (object)r.Standpipe ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Sprinkler", (object)r.Sprinkler ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@FireAlarm", (object)r.FireAlarm ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Equipment", (object)r.Equipment ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@FireSuppression", (object)r.FireSuppression ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@CurbCut", (object)r.CurbCut ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Other", (object)r.Other ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@OtherDescription", (object)r.OtherDescription ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ApplicantFirstName", (object)r.ApplicantFirstName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ApplicantLastName", (object)r.ApplicantLastName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ApplicantProfessionalTitle", (object)r.ApplicantProfessionalTitle ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ApplicantLicenseNum", (object)r.ApplicantLicenseNumber ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ProfessionalCert", (object)r.ProfessionalCert ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@PreFilingDate", (object)ParseDate(r.PreFilingDate) ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Paid", (object)ParseDate(r.Paid) ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@FullyPaid", (object)ParseDate(r.FullyPaid) ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Assigned", (object)ParseDate(r.Assigned) ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Approved", (object)ParseDate(r.Approved) ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@FullyPermitted", (object)ParseDate(r.FullyPermitted) ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@InitialCost", (object)ParseCurrency(r.InitialCost) ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@TotalEstFee", (object)ParseCurrency(r.TotalEstFee) ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@FeeStatus", (object)r.FeeStatus ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ExistingZoningSqft", (object)r.ExistingZoningSqft ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ProposedZoningSqft", (object)r.ProposedZoningSqft ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@HorizontalEnlrgmt", (object)r.HorizontalEnlrgmt ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@VerticalEnlrgmt", (object)r.VerticalEnlrgmt ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@EnlargementSqFootage", (object)r.EnlargementSqFootage ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@StreetFrontage", (object)r.StreetFrontage ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ExistingNoOfStories", (object)r.ExistingNoOfStories ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ProposedNoOfStories", (object)r.ProposedNoOfStories ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ExistingHeight", (object)r.ExistingHeight ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ProposedHeight", (object)r.ProposedHeight ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ExistingDwellingUnits", (object)r.ExistingDwellingUnits ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ProposedDwellingUnits", (object)r.ProposedDwellingUnits ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ExistingOccupancy", (object)r.ExistingOccupancy ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ProposedOccupancy", (object)r.ProposedOccupancy ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@SiteFill", (object)r.SiteFill ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ZoningDist1", (object)r.ZoningDist1 ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ZoningDist2", (object)r.ZoningDist2 ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ZoningDist3", (object)r.ZoningDist3 ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@SpecialDistrict1", (object)r.SpecialDistrict1 ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@SpecialDistrict2", (object)r.SpecialDistrict2 ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@OwnerType", (object)r.OwnerType ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@NonProfit", (object)r.NonProfit ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@OwnerFirstName", (object)r.OwnerFirstName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@OwnerLastName", (object)r.OwnerLastName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@OwnerBusinessName", (object)r.OwnerBusinessName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@OwnerHouseNumber", (object)r.OwnerHouseNumber ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@OwnerHouseStreetName", (object)r.OwnerHouseStreetName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@City", (object)r.City ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@State", (object)r.State ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Zip", (object)r.Zip ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@OwnerPhone", (object)r.OwnerPhone ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@JobDescription", (object)r.JobDescription ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@DobRunDate", (object)ParseIsoDate(r.DobRunDate) ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@JobS1No", (object)r.JobS1No ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@TotalConstructionFloorArea", (object)r.TotalConstructionFloorArea ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@WithdrawalFlag", (object)r.WithdrawalFlag ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@SignoffDate", (object)ParseDate(r.SignoffDate) ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@SpecialActionStatus", (object)r.SpecialActionStatus ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@SpecialActionDate", (object)r.SpecialActionDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@BuildingClass", (object)r.BuildingClass ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@JobNoGoodCount", (object)r.JobNoGoodCount ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@GisLatitude", (object)r.GisLatitude ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@GisLongitude", (object)r.GisLongitude ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@GisCouncilDistrict", (object)r.GisCouncilDistrict ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@GisCensusTract", (object)r.GisCensusTract ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@GisNtaName", (object)r.GisNtaName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@GisBin", (object)r.GisBin ?? DBNull.Value);

        // Compute lead score inline
        cmd.Parameters.AddWithValue("@LeadScore", ComputeLeadScore(r));
    }

    /// <summary>
    /// Logs an ingestion run to the IngestionLogs table.
    /// </summary>
    public async Task LogIngestionRun(
        int inserted, int updated, int skipped,
        string status, string errorMessage, DateTime? lastTimestamp)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"
            INSERT INTO IngestionLogs (RunDate, RecordsIngested, RecordsUpdated, RecordsSkipped, Status, ErrorMessage, LastSocrataTimestamp)
            VALUES (@RunDate, @RecordsIngested, @RecordsUpdated, @RecordsSkipped, @Status, @ErrorMessage, @LastSocrataTimestamp)";

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@RunDate", DateTime.UtcNow);
        cmd.Parameters.AddWithValue("@RecordsIngested", inserted);
        cmd.Parameters.AddWithValue("@RecordsUpdated", updated);
        cmd.Parameters.AddWithValue("@RecordsSkipped", skipped);
        cmd.Parameters.AddWithValue("@Status", status);
        cmd.Parameters.AddWithValue("@ErrorMessage", (object)errorMessage ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@LastSocrataTimestamp", (object)lastTimestamp ?? DBNull.Value);

        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Gets the last successful ingestion timestamp (watermark for delta loads).
    /// </summary>
    public async Task<DateTime?> GetLastIngestionTimestamp()
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT TOP 1 LastSocrataTimestamp
            FROM IngestionLogs
            WHERE Status = 'Success' AND LastSocrataTimestamp IS NOT NULL
            ORDER BY RunDate DESC";

        await using var cmd = new SqlCommand(sql, connection);
        var result = await cmd.ExecuteScalarAsync();
        return result as DateTime?;
    }

    public async Task<(int Inserted, int Updated, int Skipped)> UpsertDobViolations(IReadOnlyList<SocrataDobViolationRecord> records)
    {
        int inserted = 0, updated = 0, skipped = 0;
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        foreach (var record in records)
        {
            try
            {
                if (string.IsNullOrEmpty(record.IsnDobBisViol)) { skipped++; continue; }

                const string mergeSql = @"
                    MERGE DOB_Violations AS target
                    USING (SELECT @IsnDobBisViol AS isn_dob_bis_viol) AS source
                    ON target.isn_dob_bis_viol = source.isn_dob_bis_viol
                    WHEN MATCHED THEN UPDATE SET
                        boro = @Boro, bin = @Bin, block = @Block, lot = @Lot, issue_date = @IssueDate,
                        violation_type_code = @ViolationTypeCode, violation_number = @ViolationNumber,
                        house_number = @HouseNumber, street = @Street, description = @Description,
                        number = @Number, violation_category = @ViolationCategory, violation_type = @ViolationType
                    WHEN NOT MATCHED THEN INSERT (
                        isn_dob_bis_viol, boro, bin, block, lot, issue_date, violation_type_code,
                        violation_number, house_number, street, description, number, violation_category, violation_type
                    ) VALUES (
                        @IsnDobBisViol, @Boro, @Bin, @Block, @Lot, @IssueDate, @ViolationTypeCode,
                        @ViolationNumber, @HouseNumber, @Street, @Description, @Number, @ViolationCategory, @ViolationType
                    ) OUTPUT $action;";

                await using var cmd = new SqlCommand(mergeSql, connection);
                cmd.Parameters.AddWithValue("@IsnDobBisViol", record.IsnDobBisViol);
                cmd.Parameters.AddWithValue("@Boro", ParseInt(record.Boro) ?? 0);
                cmd.Parameters.AddWithValue("@Bin", (object)record.Bin ?? string.Empty);
                cmd.Parameters.AddWithValue("@Block", (object)record.Block ?? string.Empty);
                cmd.Parameters.AddWithValue("@Lot", (object)record.Lot ?? string.Empty);
                cmd.Parameters.AddWithValue("@IssueDate", ParseDobDate(record.IssueDate) ?? new DateTime(1900, 1, 1));
                cmd.Parameters.AddWithValue("@ViolationTypeCode", (object)record.ViolationTypeCode ?? string.Empty);
                cmd.Parameters.AddWithValue("@ViolationNumber", (object)record.ViolationNumber ?? string.Empty);
                cmd.Parameters.AddWithValue("@HouseNumber", (object)record.HouseNumber ?? string.Empty);
                cmd.Parameters.AddWithValue("@Street", (object)record.Street ?? string.Empty);
                cmd.Parameters.AddWithValue("@Description", (object)record.Description ?? string.Empty);
                cmd.Parameters.AddWithValue("@Number", (object)record.Number ?? string.Empty);
                cmd.Parameters.AddWithValue("@ViolationCategory", (object)record.ViolationCategory ?? string.Empty);
                cmd.Parameters.AddWithValue("@ViolationType", (object)record.ViolationType ?? string.Empty);

                var action = (string)await cmd.ExecuteScalarAsync();
                if (action == "INSERT") inserted++;
                else if (action == "UPDATE") updated++;
                else skipped++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to upsert DOB violation {Id}", record.IsnDobBisViol);
                skipped++;
            }
        }
        return (inserted, updated, skipped);
    }

    public async Task<(int Inserted, int Updated, int Skipped)> UpsertHpdViolations(IReadOnlyList<SocrataHpdViolationRecord> records)
    {
        int inserted = 0, updated = 0, skipped = 0;
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        foreach (var record in records)
        {
            try
            {
                if (string.IsNullOrEmpty(record.ViolationId)) { skipped++; continue; }

                const string mergeSql = @"
                    MERGE HPD_Violations AS target
                    USING (SELECT @ViolationId AS violation_id) AS source
                    ON target.violation_id = source.violation_id
                    WHEN MATCHED THEN UPDATE SET
                        building_id = @BuildingId, boro = @Boro, boro_id = @BoroId, bin = @Bin,
                        block = @Block, lot = @Lot, house_number = @HouseNumber, street_name = @StreetName,
                        zip = @Zip, apartment = @Apartment, inspection_date = @InspectionDate,
                        approved_date = @ApprovedDate, original_certify_by_date = @OriginalCertifyByDate,
                        original_correct_by_date = @OriginalCorrectByDate, violation_status = @ViolationStatus,
                        violation_type = @ViolationType, nov_description = @NovDescription, class = @Class
                    WHEN NOT MATCHED THEN INSERT (
                        violation_id, building_id, boro, boro_id, bin, block, lot, house_number, street_name,
                        zip, apartment, inspection_date, approved_date, original_certify_by_date,
                        original_correct_by_date, violation_status, violation_type, nov_description, class
                    ) VALUES (
                        @ViolationId, @BuildingId, @Boro, @BoroId, @Bin, @Block, @Lot, @HouseNumber, @StreetName,
                        @Zip, @Apartment, @InspectionDate, @ApprovedDate, @OriginalCertifyByDate,
                        @OriginalCorrectByDate, @ViolationStatus, @ViolationType, @NovDescription, @Class
                    ) OUTPUT $action;";

                await using var cmd = new SqlCommand(mergeSql, connection);
                cmd.Parameters.AddWithValue("@ViolationId", record.ViolationId);
                cmd.Parameters.AddWithValue("@BuildingId", (object)record.BuildingId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Boro", (object)record.Boro ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@BoroId", (object)record.BoroId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Bin", (object)record.Bin ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Block", (object)record.Block ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Lot", (object)record.Lot ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@HouseNumber", (object)record.HouseNumber ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@StreetName", (object)record.StreetName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Zip", (object)record.Zip ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Apartment", (object)record.Apartment ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@InspectionDate", (object)ParseIsoDate(record.InspectionDate) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ApprovedDate", (object)ParseIsoDate(record.ApprovedDate) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@OriginalCertifyByDate", (object)ParseIsoDate(record.OriginalCertifyByDate) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@OriginalCorrectByDate", (object)ParseIsoDate(record.OriginalCorrectByDate) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ViolationStatus", (object)record.ViolationStatus ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ViolationType", (object)record.ViolationType ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@NovDescription", (object)record.NovDescription ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Class", (object)record.Class ?? DBNull.Value);

                var action = (string)await cmd.ExecuteScalarAsync();
                if (action == "INSERT") inserted++;
                else if (action == "UPDATE") updated++;
                else skipped++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to upsert HPD violation {Id}", record.ViolationId);
                skipped++;
            }
        }
        return (inserted, updated, skipped);
    }

    // ── Parsing Helpers ──

    private static int? ParseInt(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return int.TryParse(value, out var result) ? result : null;
    }

    private static DateTime? ParseDate(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        // Socrata returns dates as "MM/dd/yyyy"
        if (DateTime.TryParseExact(value, "MM/dd/yyyy", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var result))
            return result;
        // Fallback to general parsing
        if (DateTime.TryParse(value, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out result))
            return result;
        return null;
    }

    private static DateTime? ParseIsoDate(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        // Socrata returns ISO 8601: "2022-02-24T00:00:00.000"
        if (DateTime.TryParse(value, CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind, out var result))
            return result;
        return null;
    }

    private static DateTime? ParseDobDate(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (DateTime.TryParseExact(value, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
            return result;
        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out result))
            return result;
        return null;
    }

    private static decimal? ParseCurrency(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        // Strip "$" and "," → parse as decimal
        var cleaned = value.Replace("$", "").Replace(",", "").Trim();
        return decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.InvariantCulture, out var result)
            ? result
            : null;
    }

    private static int ComputeLeadScore(SocrataPermitRecord r)
    {
        int score = 0;
        var cost = ParseCurrency(r.InitialCost);

        if (cost.HasValue && cost.Value > 10_000m) score++;
        if (cost.HasValue && cost.Value > 50_000m) score++;
        if (r.JobType is "A1" or "NB") score++;

        int tradeCount = 0;
        if (r.Plumbing == "X") tradeCount++;
        if (r.Mechanical == "X") tradeCount++;
        if (r.Boiler == "X") tradeCount++;
        if (r.Sprinkler == "X") tradeCount++;
        if (r.FireAlarm == "X") tradeCount++;
        if (r.FireSuppression == "X") tradeCount++;
        if (r.Equipment == "X") tradeCount++;
        if (r.Standpipe == "X") tradeCount++;
        if (tradeCount >= 2) score++;

        if (int.TryParse(r.ProposedDwellingUnits, out var proposed) &&
            int.TryParse(r.ExistingDwellingUnits, out var existing) &&
            proposed > existing)
            score++;

        return Math.Clamp(score, 1, 5);
    }

    private enum UpsertResult { Inserted, Updated, Skipped }
}

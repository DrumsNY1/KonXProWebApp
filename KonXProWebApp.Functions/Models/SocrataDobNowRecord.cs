using System.Text.Json.Serialization;

namespace KonXProWebApp.Functions.Models;

/// <summary>
/// Maps to the NYC DOB NOW: Build – Job Application Filings Socrata API response.
/// Dataset: w9ak-ipjd
/// This dataset captures permits at the earliest stage (filing/application).
/// </summary>
public class SocrataDobNowRecord
{
    [JsonPropertyName("job_filing_number")]
    public string JobFilingNumber { get; set; }

    [JsonPropertyName("filing_status")]
    public string FilingStatus { get; set; }

    [JsonPropertyName("house_no")]
    public string HouseNumber { get; set; }

    [JsonPropertyName("street_name")]
    public string StreetName { get; set; }

    [JsonPropertyName("borough")]
    public string Borough { get; set; }

    [JsonPropertyName("block")]
    public string Block { get; set; }

    [JsonPropertyName("lot")]
    public string Lot { get; set; }

    [JsonPropertyName("bin")]
    public string Bin { get; set; }

    [JsonPropertyName("bbl")]
    public string Bbl { get; set; }

    [JsonPropertyName("commmunity_board")]
    public string CommunityBoard { get; set; }

    [JsonPropertyName("job_type")]
    public string JobType { get; set; }

    [JsonPropertyName("work_on_floor")]
    public string WorkOnFloor { get; set; }

    [JsonPropertyName("filing_date")]
    public string FilingDate { get; set; }

    [JsonPropertyName("approved_date")]
    public string ApprovedDate { get; set; }

    [JsonPropertyName("first_permit_date")]
    public string FirstPermitDate { get; set; }

    [JsonPropertyName("current_status_date")]
    public string CurrentStatusDate { get; set; }

    [JsonPropertyName("signoff_date")]
    public string SignoffDate { get; set; }

    [JsonPropertyName("initial_cost")]
    public string InitialCost { get; set; }

    [JsonPropertyName("job_description")]
    public string JobDescription { get; set; }

    [JsonPropertyName("building_type")]
    public string BuildingType { get; set; }

    // Owner info
    [JsonPropertyName("owner_first_name")]
    public string OwnerFirstName { get; set; }

    [JsonPropertyName("owner_last_name")]
    public string OwnerLastName { get; set; }

    [JsonPropertyName("owner_s_business_name")]
    public string OwnerBusinessName { get; set; }

    [JsonPropertyName("owner_type")]
    public string OwnerType { get; set; }

    // Applicant info
    [JsonPropertyName("applicant_first_name")]
    public string ApplicantFirstName { get; set; }

    [JsonPropertyName("applicant_last_name")]
    public string ApplicantLastName { get; set; }

    [JsonPropertyName("applicant_business_name")]
    public string ApplicantBusinessName { get; set; }

    [JsonPropertyName("applicant_professional_title")]
    public string ApplicantProfessionalTitle { get; set; }

    [JsonPropertyName("applicant_license")]
    public string ApplicantLicense { get; set; }

    // Trade flags (YES/NO strings)
    [JsonPropertyName("plumbing_work_type")]
    public string Plumbing { get; set; }

    [JsonPropertyName("sprinkler_work_type")]
    public string Sprinkler { get; set; }

    [JsonPropertyName("standpipe")]
    public string Standpipe { get; set; }

    [JsonPropertyName("boiler_equipment_work_type_")]
    public string Boiler { get; set; }

    [JsonPropertyName("mechanical_systems_work_type_")]
    public string Mechanical { get; set; }

    [JsonPropertyName("fire_alarm_work_type_")]
    public string FireAlarm { get; set; }

    [JsonPropertyName("fire_suppression_work_type_")]
    public string FireSuppression { get; set; }

    [JsonPropertyName("curb_cut")]
    public string CurbCut { get; set; }

    // Location
    [JsonPropertyName("latitude")]
    public string Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public string Longitude { get; set; }

    [JsonPropertyName("council_district")]
    public string CouncilDistrict { get; set; }

    [JsonPropertyName("census_tract")]
    public string CensusTract { get; set; }

    [JsonPropertyName("nta")]
    public string Nta { get; set; }

    [JsonPropertyName("existing_dwelling_units")]
    public string ExistingDwellingUnits { get; set; }

    [JsonPropertyName("proposed_dwelling_units")]
    public string ProposedDwellingUnits { get; set; }

    [JsonPropertyName("total_construction_floor_area")]
    public string TotalConstructionFloorArea { get; set; }

    [JsonPropertyName("zip")]
    public string Zip { get; set; }

    [JsonPropertyName("city")]
    public string City { get; set; }

    [JsonPropertyName("state")]
    public string State { get; set; }
}

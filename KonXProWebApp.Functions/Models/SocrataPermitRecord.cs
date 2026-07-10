using System.Text.Json.Serialization;

namespace KonXProWebApp.Functions.Models;

/// <summary>
/// Maps to the NYC DOB Job Application Filings Socrata API response.
/// Dataset: ic3t-wcy2
/// </summary>
public class SocrataPermitRecord
{
    [JsonPropertyName("job__")]
    public string JobNumber { get; set; }

    [JsonPropertyName("doc__")]
    public string DocNumber { get; set; }

    [JsonPropertyName("borough")]
    public string Borough { get; set; }

    [JsonPropertyName("house__")]
    public string HouseNumber { get; set; }

    [JsonPropertyName("street_name")]
    public string StreetName { get; set; }

    [JsonPropertyName("block")]
    public string Block { get; set; }

    [JsonPropertyName("lot")]
    public string Lot { get; set; }

    [JsonPropertyName("bin__")]
    public string Bin { get; set; }

    [JsonPropertyName("job_type")]
    public string JobType { get; set; }

    [JsonPropertyName("job_status")]
    public string JobStatus { get; set; }

    [JsonPropertyName("job_status_descrp")]
    public string JobStatusDescription { get; set; }

    [JsonPropertyName("latest_action_date")]
    public string LatestActionDate { get; set; }

    [JsonPropertyName("building_type")]
    public string BuildingType { get; set; }

    [JsonPropertyName("community___board")]
    public string CommunityBoard { get; set; }

    [JsonPropertyName("cluster")]
    public string Cluster { get; set; }

    [JsonPropertyName("landmarked")]
    public string Landmarked { get; set; }

    [JsonPropertyName("adult_estab")]
    public string AdultEstab { get; set; }

    [JsonPropertyName("loft_board")]
    public string LoftBoard { get; set; }

    [JsonPropertyName("city_owned")]
    public string CityOwned { get; set; }

    [JsonPropertyName("little_e")]
    public string LittleE { get; set; }

    [JsonPropertyName("pc_filed")]
    public string PcFiled { get; set; }

    [JsonPropertyName("efiling_filed")]
    public string EFilingFiled { get; set; }

    [JsonPropertyName("plumbing")]
    public string Plumbing { get; set; }

    [JsonPropertyName("mechanical")]
    public string Mechanical { get; set; }

    [JsonPropertyName("boiler")]
    public string Boiler { get; set; }

    [JsonPropertyName("fuel_burning")]
    public string FuelBurning { get; set; }

    [JsonPropertyName("fuel_storage")]
    public string FuelStorage { get; set; }

    [JsonPropertyName("standpipe")]
    public string Standpipe { get; set; }

    [JsonPropertyName("sprinkler")]
    public string Sprinkler { get; set; }

    [JsonPropertyName("fire_alarm")]
    public string FireAlarm { get; set; }

    [JsonPropertyName("equipment_")]
    public string Equipment { get; set; }

    [JsonPropertyName("fire_suppression")]
    public string FireSuppression { get; set; }

    [JsonPropertyName("curb_cut")]
    public string CurbCut { get; set; }

    [JsonPropertyName("other")]
    public string Other { get; set; }

    [JsonPropertyName("other_description")]
    public string OtherDescription { get; set; }

    [JsonPropertyName("applicant_s_first_name")]
    public string ApplicantFirstName { get; set; }

    [JsonPropertyName("applicant_s_last_name")]
    public string ApplicantLastName { get; set; }

    [JsonPropertyName("applicant_professional_title")]
    public string ApplicantProfessionalTitle { get; set; }

    [JsonPropertyName("applicant_license__")]
    public string ApplicantLicenseNumber { get; set; }

    [JsonPropertyName("professional_cert")]
    public string ProfessionalCert { get; set; }

    [JsonPropertyName("pre__filing_date")]
    public string PreFilingDate { get; set; }

    [JsonPropertyName("paid")]
    public string Paid { get; set; }

    [JsonPropertyName("fully_paid")]
    public string FullyPaid { get; set; }

    [JsonPropertyName("assigned")]
    public string Assigned { get; set; }

    [JsonPropertyName("approved")]
    public string Approved { get; set; }

    [JsonPropertyName("fully_permitted")]
    public string FullyPermitted { get; set; }

    [JsonPropertyName("initial_cost")]
    public string InitialCost { get; set; }

    [JsonPropertyName("total_est__fee")]
    public string TotalEstFee { get; set; }

    [JsonPropertyName("fee_status")]
    public string FeeStatus { get; set; }

    [JsonPropertyName("existing_zoning_sqft")]
    public string ExistingZoningSqft { get; set; }

    [JsonPropertyName("proposed_zoning_sqft")]
    public string ProposedZoningSqft { get; set; }

    [JsonPropertyName("horizontal_enlrgmt")]
    public string HorizontalEnlrgmt { get; set; }

    [JsonPropertyName("vertical_enlrgmt")]
    public string VerticalEnlrgmt { get; set; }

    [JsonPropertyName("enlargement_sq_footage")]
    public string EnlargementSqFootage { get; set; }

    [JsonPropertyName("street_frontage")]
    public string StreetFrontage { get; set; }

    [JsonPropertyName("existingno_of_stories")]
    public string ExistingNoOfStories { get; set; }

    [JsonPropertyName("proposed_no_of_stories")]
    public string ProposedNoOfStories { get; set; }

    [JsonPropertyName("existing_height")]
    public string ExistingHeight { get; set; }

    [JsonPropertyName("proposed_height")]
    public string ProposedHeight { get; set; }

    [JsonPropertyName("existing_dwelling_units")]
    public string ExistingDwellingUnits { get; set; }

    [JsonPropertyName("proposed_dwelling_units")]
    public string ProposedDwellingUnits { get; set; }

    [JsonPropertyName("existing_occupancy")]
    public string ExistingOccupancy { get; set; }

    [JsonPropertyName("proposed_occupancy")]
    public string ProposedOccupancy { get; set; }

    [JsonPropertyName("site_fill")]
    public string SiteFill { get; set; }

    [JsonPropertyName("zoning_dist1")]
    public string ZoningDist1 { get; set; }

    [JsonPropertyName("zoning_dist2")]
    public string ZoningDist2 { get; set; }

    [JsonPropertyName("zoning_dist3")]
    public string ZoningDist3 { get; set; }

    [JsonPropertyName("special_district_1")]
    public string SpecialDistrict1 { get; set; }

    [JsonPropertyName("special_district_2")]
    public string SpecialDistrict2 { get; set; }

    [JsonPropertyName("owner_type")]
    public string OwnerType { get; set; }

    [JsonPropertyName("non_profit")]
    public string NonProfit { get; set; }

    [JsonPropertyName("owner_s_first_name")]
    public string OwnerFirstName { get; set; }

    [JsonPropertyName("owner_s_last_name")]
    public string OwnerLastName { get; set; }

    [JsonPropertyName("owner_s_business_name")]
    public string OwnerBusinessName { get; set; }

    [JsonPropertyName("owner_s_house_number")]
    public string OwnerHouseNumber { get; set; }

    [JsonPropertyName("owner_s__house_street_name")]
    public string OwnerHouseStreetName { get; set; }

    [JsonPropertyName("city")]
    public string City { get; set; }

    [JsonPropertyName("state")]
    public string State { get; set; }

    [JsonPropertyName("zip")]
    public string Zip { get; set; }

    [JsonPropertyName("owner_s_phone__")]
    public string OwnerPhone { get; set; }

    [JsonPropertyName("job_description")]
    public string JobDescription { get; set; }

    [JsonPropertyName("dobrundate")]
    public string DobRunDate { get; set; }

    [JsonPropertyName("job_s1_no")]
    public string JobS1No { get; set; }

    [JsonPropertyName("total_construction_floor_area")]
    public string TotalConstructionFloorArea { get; set; }

    [JsonPropertyName("withdrawal_flag")]
    public string WithdrawalFlag { get; set; }

    [JsonPropertyName("signoff_date")]
    public string SignoffDate { get; set; }

    [JsonPropertyName("special_action_status")]
    public string SpecialActionStatus { get; set; }

    [JsonPropertyName("special_action_date")]
    public string SpecialActionDate { get; set; }

    [JsonPropertyName("building_class")]
    public string BuildingClass { get; set; }

    [JsonPropertyName("job_no_good_count")]
    public string JobNoGoodCount { get; set; }

    [JsonPropertyName("gis_latitude")]
    public string GisLatitude { get; set; }

    [JsonPropertyName("gis_longitude")]
    public string GisLongitude { get; set; }

    [JsonPropertyName("gis_council_district")]
    public string GisCouncilDistrict { get; set; }

    [JsonPropertyName("gis_census_tract")]
    public string GisCensusTract { get; set; }

    [JsonPropertyName("gis_nta_name")]
    public string GisNtaName { get; set; }

    [JsonPropertyName("gis_bin")]
    public string GisBin { get; set; }
}

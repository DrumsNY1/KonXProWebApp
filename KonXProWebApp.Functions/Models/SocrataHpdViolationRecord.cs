using System.Text.Json.Serialization;

namespace KonXProWebApp.Functions.Models;

public class SocrataHpdViolationRecord
{
    [JsonPropertyName("violationid")]
    public string ViolationId { get; set; }

    [JsonPropertyName("buildingid")]
    public string BuildingId { get; set; }

    [JsonPropertyName("boro")]
    public string Boro { get; set; }

    [JsonPropertyName("boroid")]
    public string BoroId { get; set; }

    [JsonPropertyName("bin")]
    public string Bin { get; set; }

    [JsonPropertyName("block")]
    public string Block { get; set; }

    [JsonPropertyName("lot")]
    public string Lot { get; set; }

    [JsonPropertyName("housenumber")]
    public string HouseNumber { get; set; }

    [JsonPropertyName("streetname")]
    public string StreetName { get; set; }

    [JsonPropertyName("zip")]
    public string Zip { get; set; }

    [JsonPropertyName("apartment")]
    public string Apartment { get; set; }

    [JsonPropertyName("inspectiondate")]
    public string InspectionDate { get; set; }

    [JsonPropertyName("approveddate")]
    public string ApprovedDate { get; set; }

    [JsonPropertyName("originalcertifybydate")]
    public string OriginalCertifyByDate { get; set; }

    [JsonPropertyName("originalcorrectbydate")]
    public string OriginalCorrectByDate { get; set; }

    [JsonPropertyName("currentstatus")]
    public string ViolationStatus { get; set; }

    [JsonPropertyName("novdescription")]
    public string NovDescription { get; set; }

    [JsonPropertyName("class")]
    public string Class { get; set; }

    [JsonPropertyName("violationtype")]
    public string ViolationType { get; set; }
}

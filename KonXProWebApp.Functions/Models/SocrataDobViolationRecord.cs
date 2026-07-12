using System.Text.Json.Serialization;

namespace KonXProWebApp.Functions.Models;

public class SocrataDobViolationRecord
{
    [JsonPropertyName("isn_dob_bis_viol")]
    public string IsnDobBisViol { get; set; }

    [JsonPropertyName("boro")]
    public string Boro { get; set; }

    [JsonPropertyName("bin")]
    public string Bin { get; set; }

    [JsonPropertyName("block")]
    public string Block { get; set; }

    [JsonPropertyName("lot")]
    public string Lot { get; set; }

    [JsonPropertyName("issue_date")]
    public string IssueDate { get; set; }

    [JsonPropertyName("violation_type_code")]
    public string ViolationTypeCode { get; set; }

    [JsonPropertyName("violation_number")]
    public string ViolationNumber { get; set; }

    [JsonPropertyName("house_number")]
    public string HouseNumber { get; set; }

    [JsonPropertyName("street")]
    public string Street { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("number")]
    public string Number { get; set; }

    [JsonPropertyName("violation_category")]
    public string ViolationCategory { get; set; }

    [JsonPropertyName("violation_type")]
    public string ViolationType { get; set; }
}

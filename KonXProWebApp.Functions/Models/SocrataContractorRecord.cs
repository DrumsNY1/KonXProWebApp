using System.Text.Json.Serialization;

namespace KonXProWebApp.Functions.Models;

public class SocrataContractorRecord
{
    [JsonPropertyName("license_nbr")]
    public string LicenseNumber { get; set; }

    [JsonPropertyName("business_name")]
    public string BusinessName { get; set; }

    [JsonPropertyName("dba_trade_name")]
    public string DbaTradeName { get; set; }

    [JsonPropertyName("business_unique_id")]
    public string BusinessUniqueId { get; set; }

    [JsonPropertyName("license_status")]
    public string LicenseStatus { get; set; }

    [JsonPropertyName("license_creation_date")]
    public string LicenseIssueDate { get; set; }

    [JsonPropertyName("license_expiration_date")]
    public string LicenseExpirationDate { get; set; }

    [JsonPropertyName("contact_phone_number")]
    public string ContactPhone { get; set; }

    [JsonPropertyName("address_building")]
    public string AddressBuilding { get; set; }

    [JsonPropertyName("address_street_name")]
    public string AddressStreetName { get; set; }

    [JsonPropertyName("address_city")]
    public string AddressCity { get; set; }

    [JsonPropertyName("address_state")]
    public string AddressState { get; set; }

    [JsonPropertyName("address_zip")]
    public string AddressZip { get; set; }

    [JsonPropertyName("address_borough")]
    public string AddressBorough { get; set; }
}

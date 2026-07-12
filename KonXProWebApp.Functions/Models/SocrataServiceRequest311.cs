using System;
using System.Text.Json.Serialization;

namespace KonXProWebApp.Functions.Models
{
    public class SocrataServiceRequest311
    {
        [JsonPropertyName("unique_key")]
        public string UniqueKey { get; set; }

        [JsonPropertyName("created_date")]
        public DateTime? CreatedDate { get; set; }

        [JsonPropertyName("closed_date")]
        public DateTime? ClosedDate { get; set; }

        [JsonPropertyName("agency")]
        public string Agency { get; set; }

        [JsonPropertyName("complaint_type")]
        public string ComplaintType { get; set; }

        [JsonPropertyName("descriptor")]
        public string Descriptor { get; set; }

        [JsonPropertyName("incident_zip")]
        public string IncidentZip { get; set; }

        [JsonPropertyName("incident_address")]
        public string IncidentAddress { get; set; }

        [JsonPropertyName("borough")]
        public string Borough { get; set; }

        [JsonPropertyName("bbl")]
        public string Bbl { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }
    }
}

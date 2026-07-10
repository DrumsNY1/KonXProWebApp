using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KonXProWebApp.Models.PermitIntel;

[Table("AlertPreferences", Schema = "dbo")]
public partial class AlertPreference
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; }

    [MaxLength(200)]
    public string Boroughs { get; set; }  // Comma-separated: "BROOKLYN,QUEENS"

    [MaxLength(200)]
    public string JobTypes { get; set; }  // "A1,A2,A3,NB"

    [MaxLength(500)]
    public string Trades { get; set; }  // "Plumbing,Mechanical,Boiler"

    public decimal? MinCost { get; set; }

    public decimal? MaxCost { get; set; }

    [Required]
    [MaxLength(50)]
    public string AlertChannel { get; set; } = "Email";  // "Email", "SMS", "Both"

    [Required]
    [MaxLength(50)]
    public string AlertFrequency { get; set; } = "Daily";  // "Daily", "Instant"

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

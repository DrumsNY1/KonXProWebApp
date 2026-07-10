using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KonXProWebApp.Models.PermitIntel;

[Table("SavedLeads", Schema = "dbo")]
public partial class SavedLead
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; }

    public int DobjobFilingId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "New";  // "New", "Contacted", "Quoted", "Won", "Lost"

    public string Notes { get; set; }

    public DateTime SavedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(DobjobFilingId))]
    public KonXProWebApp.Models.db_9f8bee_konxdev.DobjobFiling DobjobFiling { get; set; }
}

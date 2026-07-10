using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KonXProWebApp.Models.PermitIntel;

[Table("IngestionLogs", Schema = "dbo")]
public partial class IngestionLog
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public DateTime RunDate { get; set; } = DateTime.UtcNow;

    public int RecordsIngested { get; set; }

    public int RecordsUpdated { get; set; }

    public int RecordsSkipped { get; set; }

    [Required]
    [MaxLength(50)]
    public string Status { get; set; }  // "Success", "PartialFailure", "Failed"

    public string ErrorMessage { get; set; }

    public DateTime? LastSocrataTimestamp { get; set; }
}

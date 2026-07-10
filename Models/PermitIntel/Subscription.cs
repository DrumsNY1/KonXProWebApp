using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KonXProWebApp.Models.PermitIntel;

[Table("Subscriptions", Schema = "dbo")]
public partial class Subscription
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; }

    public string StripeCustomerId { get; set; }

    public string StripeSubscriptionId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Tier { get; set; }  // "Starter", "Pro", "Business", "Agency"

    [Required]
    [MaxLength(50)]
    public string Status { get; set; }  // "Active", "Canceled", "PastDue", "Trialing"

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public DateTime? TrialEndDate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KonXProWebApp.Models.db_9f8bee_konxdev
{
    [Table("HomeImprovementContractors")]
    public partial class HomeImprovementContractor
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [StringLength(50)]
        public string LicenseNumber { get; set; }

        [StringLength(255)]
        public string BusinessName { get; set; }

        [StringLength(255)]
        public string DbaTradeName { get; set; }

        [StringLength(50)]
        public string BusinessUniqueId { get; set; }

        [StringLength(50)]
        public string LicenseStatus { get; set; }

        public DateTime? LicenseIssueDate { get; set; }

        public DateTime? LicenseExpirationDate { get; set; }

        [StringLength(50)]
        public string ContactPhoneNumber { get; set; }

        [StringLength(100)]
        public string AddressBuilding { get; set; }

        [StringLength(255)]
        public string AddressStreetName { get; set; }

        [StringLength(100)]
        public string AddressCity { get; set; }

        [StringLength(50)]
        public string AddressState { get; set; }

        [StringLength(20)]
        public string AddressZip { get; set; }

        [StringLength(100)]
        public string Borough { get; set; }

        public DateTime IngestedAt { get; set; } = DateTime.UtcNow;
    }
}

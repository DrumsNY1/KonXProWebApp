using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KonXProWebApp.Models.db_9f8bee_konxdev
{
    [Table("HPD_Violations", Schema = "dbo")]
    public partial class HpdViolation
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }

        [Column("violation_id")]
        public string ViolationId { get; set; }

        [Column("building_id")]
        public string BuildingId { get; set; }

        [Column("boro")]
        public string Boro { get; set; }

        [Column("boro_id")]
        public string BoroId { get; set; }

        [Column("bin")]
        public string Bin { get; set; }

        [Column("block")]
        public string Block { get; set; }

        [Column("lot")]
        public string Lot { get; set; }

        [Column("house_number")]
        public string HouseNumber { get; set; }

        [Column("street_name")]
        public string StreetName { get; set; }

        [Column("zip")]
        public string Zip { get; set; }

        [Column("apartment")]
        public string Apartment { get; set; }

        [Column("inspection_date")]
        public DateTime? InspectionDate { get; set; }

        [Column("approved_date")]
        public DateTime? ApprovedDate { get; set; }

        [Column("original_certify_by_date")]
        public DateTime? OriginalCertifyByDate { get; set; }

        [Column("original_correct_by_date")]
        public DateTime? OriginalCorrectByDate { get; set; }

        [Column("violation_status")]
        public string ViolationStatus { get; set; }

        [Column("violation_type")]
        public string ViolationType { get; set; }

        [Column("nov_description")]
        public string NovDescription { get; set; }

        [Column("class")]
        public string Class { get; set; }
    }
}

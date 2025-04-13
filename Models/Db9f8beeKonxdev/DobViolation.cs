using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KonXProWebApp.Models.db_9f8bee_konxdev
{
    [Table("DOB_Violations", Schema = "dbo")]
    public partial class DobViolation
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }

        [Column("isn_dob_bis_viol")]
        [Required]
        public string IsnDobBisViol { get; set; }

        [Column("boro")]
        [Required]
        public int Boro { get; set; }

        [Column("bin")]
        [Required]
        public string Bin { get; set; }

        [Column("block")]
        [Required]
        public string Block { get; set; }

        [Column("lot")]
        [Required]
        public string Lot { get; set; }

        [Column("issue_date")]
        [Required]
        public DateTime IssueDate { get; set; }

        [Column("violation_type_code")]
        [Required]
        public string ViolationTypeCode { get; set; }

        [Column("violation_number")]
        [Required]
        public string ViolationNumber { get; set; }

        [Column("house_number")]
        [Required]
        public string HouseNumber { get; set; }

        [Column("street")]
        [Required]
        public string Street { get; set; }

        [Column("description")]
        [Required]
        public string Description { get; set; }

        [Column("number")]
        [Required]
        public string Number { get; set; }

        [Column("violation_category")]
        [Required]
        public string ViolationCategory { get; set; }

        [Column("violation_type")]
        [Required]
        public string ViolationType { get; set; }
    }
}
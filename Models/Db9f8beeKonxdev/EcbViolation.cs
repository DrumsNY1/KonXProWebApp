using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KonXProWebApp.Models.db_9f8bee_konxdev
{
    [Table("ECB_Violations", Schema = "dbo")]
    public partial class EcbViolation
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }

        [Column("isn_dob_bis_extract")]
        [Required]
        public string IsnDobBisExtract { get; set; }

        [Column("ecb_violation_number")]
        [Required]
        public string EcbViolationNumber { get; set; }

        [Column("ecb_violation_status")]
        [Required]
        public string EcbViolationStatus { get; set; }

        [Column("bin")]
        [Required]
        public string Bin { get; set; }

        [Column("boro")]
        [Required]
        public int Boro { get; set; }

        [Column("block")]
        [Required]
        public string Block { get; set; }

        [Column("lot")]
        [Required]
        public string Lot { get; set; }

        [Column("hearing_date")]
        [Required]
        public DateTime HearingDate { get; set; }

        [Column("hearing_time")]
        [Required]
        public string HearingTime { get; set; }

        [Column("served_date")]
        [Required]
        public DateTime ServedDate { get; set; }

        [Column("issue_date")]
        [Required]
        public DateTime IssueDate { get; set; }

        [Column("severity")]
        [Required]
        public string Severity { get; set; }

        [Column("violation_type")]
        [Required]
        public string ViolationType { get; set; }

        [Column("respondent_name")]
        [Required]
        public string RespondentName { get; set; }

        [Column("respondent_house_number")]
        [Required]
        public string RespondentHouseNumber { get; set; }

        [Column("respondent_street")]
        [Required]
        public string RespondentStreet { get; set; }

        [Column("respondent_city")]
        [Required]
        public string RespondentCity { get; set; }

        [Column("respondent_zip")]
        [Required]
        public string RespondentZip { get; set; }

        [Column("violation_description")]
        [Required]
        public string ViolationDescription { get; set; }

        [Column("penality_imposed")]
        [Required]
        public decimal PenalityImposed { get; set; }

        [Column("amount_paid")]
        [Required]
        public decimal AmountPaid { get; set; }

        [Column("balance_due")]
        [Required]
        public decimal BalanceDue { get; set; }

        [Column("infraction_code1")]
        [Required]
        public string InfractionCode1 { get; set; }

        [Column("section_law_description1")]
        [Required]
        public string SectionLawDescription1 { get; set; }

        [Column("aggravated_level")]
        [Required]
        public string AggravatedLevel { get; set; }

        [Column("hearing_status")]
        [Required]
        public string HearingStatus { get; set; }

        [Column("certification_status")]
        public string CertificationStatus { get; set; }
    }
}
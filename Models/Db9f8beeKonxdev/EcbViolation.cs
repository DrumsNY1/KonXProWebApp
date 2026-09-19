using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KonXProWebApp.Models.db_9f8bee_konxdev
{
    [Table("ECBViolations", Schema = "dbo")]
    public partial class EcbViolation
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }

        [Column("isn_dob_bis_extract")]
        [Required]
        [StringLength(20)]
        public string IsnDobBisExtract { get; set; }

        [Column("ecb_violation_number")]
        [Required]
        [StringLength(20)]
        public string EcbViolationNumber { get; set; }

        [Column("ecb_violation_status")]
        [StringLength(20)]
        public string EcbViolationStatus { get; set; }

        // Real column, confirmed via schema-truth 2026-09-19 - was entirely
        // missing from this model before. See REMEDIATION.md.
        [Column("dob_violation_number")]
        [StringLength(50)]
        public string DobViolationNumber { get; set; }

        [Column("bin")]
        [StringLength(20)]
        public string Bin { get; set; }

        // Real column is varchar(5), nullable - value converter below stores
        // this as a string, matching DobViolation.Boro's established pattern.
        [Column("boro")]
        public int? Boro { get; set; }

        [Column("block")]
        [StringLength(10)]
        public string Block { get; set; }

        [Column("lot")]
        [StringLength(10)]
        public string Lot { get; set; }

        // Real column is varchar(8) YYYYMMDD, nullable - value converter
        // below matches DobViolation.IssueDate's established pattern.
        [Column("hearing_date")]
        public DateTime? HearingDate { get; set; }

        [Column("hearing_time")]
        [StringLength(4)]
        public string HearingTime { get; set; }

        [Column("served_date")]
        public DateTime? ServedDate { get; set; }

        [Column("issue_date")]
        public DateTime? IssueDate { get; set; }

        [Column("severity")]
        [StringLength(20)]
        public string Severity { get; set; }

        [Column("violation_type")]
        [StringLength(50)]
        public string ViolationType { get; set; }

        [Column("respondent_name")]
        [StringLength(100)]
        public string RespondentName { get; set; }

        [Column("respondent_house_number")]
        [StringLength(20)]
        public string RespondentHouseNumber { get; set; }

        [Column("respondent_street")]
        [StringLength(100)]
        public string RespondentStreet { get; set; }

        [Column("respondent_city")]
        [StringLength(50)]
        public string RespondentCity { get; set; }

        [Column("respondent_zip")]
        [StringLength(10)]
        public string RespondentZip { get; set; }

        // Real column is the deprecated SQL Server `text` type - kept as
        // `string` in C#, mapped explicitly below since EF's convention
        // default (nvarchar(max)) doesn't match.
        [Column("violation_description")]
        public string ViolationDescription { get; set; }

        [Column("penality_imposed")]
        public decimal? PenalityImposed { get; set; }

        [Column("amount_paid")]
        public decimal? AmountPaid { get; set; }

        [Column("balance_due")]
        public decimal? BalanceDue { get; set; }

        [Column("infraction_code1")]
        [StringLength(10)]
        public string InfractionCode1 { get; set; }

        [Column("section_law_description1")]
        [StringLength(200)]
        public string SectionLawDescription1 { get; set; }

        [Column("aggravated_level")]
        [StringLength(5)]
        public string AggravatedLevel { get; set; }

        [Column("hearing_status")]
        [StringLength(50)]
        public string HearingStatus { get; set; }

        [Column("certification_status")]
        [StringLength(50)]
        public string CertificationStatus { get; set; }

        // Real columns, confirmed via schema-truth 2026-09-19 - both were
        // entirely missing from this model before. See REMEDIATION.md.
        [Column("created_date")]
        public DateTime? CreatedDate { get; set; }

        [Column("modified_date")]
        public DateTime? ModifiedDate { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using KonXProWebApp.Models.PermitIntel;

namespace KonXProWebApp.Models.db_9f8bee_konxdev
{
    [Table("DOBJobFilings", Schema = "dbo")]
    public partial class DobjobFiling
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int? JobNum { get; set; }

        [StringLength(50)]
        public string DocNum { get; set; }

        [StringLength(255)]
        public string Borough { get; set; }

        [StringLength(255)]
        public string HouseNum { get; set; }

        [StringLength(255)]
        public string StreetName { get; set; }

        [StringLength(50)]
        public string Block { get; set; }

        [StringLength(50)]
        public string Lot { get; set; }

        [StringLength(50)]
        public string Bin { get; set; }

        [StringLength(255)]
        public string JobType { get; set; }

        [StringLength(255)]
        public string JobStatus { get; set; }

        public string JobStatusDescrp { get; set; }

        public DateTime? LatestActionDate { get; set; }

        [StringLength(255)]
        public string BuildingType { get; set; }

        public string CommunityBoard { get; set; }

        [StringLength(255)]
        public string Cluster { get; set; }

        [StringLength(255)]
        public string Landmarked { get; set; }

        [StringLength(255)]
        public string AdultEstab { get; set; }

        [StringLength(255)]
        public string LoftBoard { get; set; }

        [StringLength(255)]
        public string CityOwned { get; set; }

        [StringLength(255)]
        public string Littlee { get; set; }

        [Column("PCFiled")]
        [StringLength(255)]
        public string Pcfiled { get; set; }

        [Column("eFilingFiled")]
        [StringLength(255)]
        public string EFilingFiled { get; set; }

        [StringLength(255)]
        public string Plumbing { get; set; }

        [StringLength(255)]
        public string Mechanical { get; set; }

        [StringLength(255)]
        public string Boiler { get; set; }

        [StringLength(255)]
        public string FuelBurning { get; set; }

        [StringLength(255)]
        public string FuelStorage { get; set; }

        [StringLength(255)]
        public string Standpipe { get; set; }

        [StringLength(255)]
        public string Sprinkler { get; set; }

        [StringLength(255)]
        public string FireAlarm { get; set; }

        [StringLength(255)]
        public string Equipment { get; set; }

        [StringLength(255)]
        public string FireSuppression { get; set; }

        [StringLength(255)]
        public string CurbCut { get; set; }

        public string Other { get; set; }

        public string OtherDescription { get; set; }

        [StringLength(255)]
        public string ApplicantsFirstName { get; set; }

        [StringLength(255)]
        public string ApplicantsLastName { get; set; }

        [StringLength(255)]
        public string ApplicantProfessionalTitle { get; set; }

        [StringLength(50)]
        public string ApplicantLicenseNum { get; set; }

        [StringLength(255)]
        public string ProfessionalCert { get; set; }

        public DateTime? PreFilingDate { get; set; }

        public DateTime? Paid { get; set; }

        public DateTime? FullyPaid { get; set; }

        public DateTime? Assigned { get; set; }

        public DateTime? Approved { get; set; }

        public DateTime? FullyPermitted { get; set; }

        // Real column is money, confirmed via schema-truth 2026-09-19 - EF's
        // convention default for an unconfigured decimal is decimal(18,2),
        // which silently mismatched the real type. See REMEDIATION.md.
        [Column(TypeName = "money")]
        public decimal? InitialCost { get; set; }

        [Column(TypeName = "money")]
        public decimal? TotalEstFee { get; set; }

        [StringLength(255)]
        public string FeeStatus { get; set; }

        [StringLength(50)]
        public string ExistingZoningSqft { get; set; }

        [StringLength(50)]
        public string ProposedZoningSqft { get; set; }

        [StringLength(255)]
        public string HorizontalEnlrgmt { get; set; }

        [StringLength(255)]
        public string VerticalEnlrgmt { get; set; }

        [Column("EnlargementSQFootage")]
        [StringLength(50)]
        public string EnlargementSqfootage { get; set; }

        [StringLength(50)]
        public string StreetFrontage { get; set; }

        [StringLength(50)]
        public string ExistingNoofStories { get; set; }

        [StringLength(50)]
        public string ProposedNoofStories { get; set; }

        [StringLength(50)]
        public string ExistingHeight { get; set; }

        [StringLength(50)]
        public string ProposedHeight { get; set; }

        [StringLength(50)]
        public string ExistingDwellingUnits { get; set; }

        [StringLength(50)]
        public string ProposedDwellingUnits { get; set; }

        [StringLength(255)]
        public string ExistingOccupancy { get; set; }

        [StringLength(255)]
        public string ProposedOccupancy { get; set; }

        [StringLength(255)]
        public string SiteFill { get; set; }

        [StringLength(255)]
        public string ZoningDist1 { get; set; }

        [StringLength(255)]
        public string ZoningDist2 { get; set; }

        [StringLength(255)]
        public string ZoningDist3 { get; set; }

        [StringLength(255)]
        public string SpecialDistrict1 { get; set; }

        [StringLength(255)]
        public string SpecialDistrict2 { get; set; }

        [StringLength(255)]
        public string OwnerType { get; set; }

        [StringLength(255)]
        public string NonProfit { get; set; }

        [StringLength(255)]
        public string OwnersFirstName { get; set; }

        [StringLength(255)]
        public string OwnersLastName { get; set; }

        [StringLength(255)]
        public string OwnersBusinessName { get; set; }

        [StringLength(50)]
        public string OwnersHouseNumber { get; set; }

        [StringLength(255)]
        public string OwnersHouseStreetName { get; set; }

        [StringLength(255)]
        public string City { get; set; }

        [StringLength(255)]
        public string State { get; set; }

        [StringLength(50)]
        public string Zip { get; set; }

        [StringLength(50)]
        public string OwnersPhone { get; set; }

        public string JobDescription { get; set; }

        [Column("DOBRunDate")]
        public DateTime? DobrunDate { get; set; }

        [Column("JOBS1NO")]
        [StringLength(50)]
        public string Jobs1no { get; set; }

        [Column("TOTALCONSTRUCTIONFLOORAREA")]
        [StringLength(50)]
        public string Totalconstructionfloorarea { get; set; }

        [Column("WITHDRAWALFLAG")]
        [StringLength(50)]
        public string Withdrawalflag { get; set; }

        [Column("SIGNOFFDATE")]
        public DateTime? Signoffdate { get; set; }

        [Column("SPECIALACTIONSTATUS")]
        [StringLength(255)]
        public string Specialactionstatus { get; set; }

        [Column("SPECIALACTIONDATE")]
        [StringLength(255)]
        public string Specialactiondate { get; set; }

        [Column("BUILDINGCLASS")]
        [StringLength(255)]
        public string Buildingclass { get; set; }

        [Column("JOBNOGOODCOUNT")]
        [StringLength(50)]
        public string Jobnogoodcount { get; set; }

        [Column("GISLATITUDE")]
        [StringLength(50)]
        public string Gislatitude { get; set; }

        [Column("GISLONGITUDE")]
        [StringLength(50)]
        public string Gislongitude { get; set; }

        [Column("GISCOUNCILDISTRICT")]
        [StringLength(50)]
        public string Giscouncildistrict { get; set; }

        [Column("GISCENSUSTRACT")]
        [StringLength(50)]
        public string Giscensustract { get; set; }

        [Column("GISNTANAME")]
        [StringLength(255)]
        public string Gisntaname { get; set; }

        [Column("GISBIN")]
        [StringLength(50)]
        public string Gisbin { get; set; }

        [Column("JobFilingNumber")]
        [StringLength(50)]
        public string JobFilingNumber { get; set; }

        [Column("DataSource")]
        [StringLength(10)]
        public string DataSource { get; set; }

        [Column("LeadScore")]
        public int? LeadScore { get; set; }

        [NotMapped]
        public LeadScoreBreakdown LeadScoreBreakdown { get; set; }

        [NotMapped]
        public string DisplayJobNumber => !string.IsNullOrWhiteSpace(JobFilingNumber) ? JobFilingNumber : JobNum?.ToString();
    }
}

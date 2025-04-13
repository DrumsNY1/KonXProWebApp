using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KonXProWebApp.Models.db_9f8bee_konxdev
{
    [Table("DOBJobFilings", Schema = "dbo")]
    public partial class DobjobFiling
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int? JobNum { get; set; }

        public string DocNum { get; set; }

        public string Borough { get; set; }

        public string HouseNum { get; set; }

        public string StreetName { get; set; }

        public string Block { get; set; }

        public string Lot { get; set; }

        public string Bin { get; set; }

        public string JobType { get; set; }

        public string JobStatus { get; set; }

        public string JobStatusDescrp { get; set; }

        public DateTime? LatestActionDate { get; set; }

        public string BuildingType { get; set; }

        public string CommunityBoard { get; set; }

        public string Cluster { get; set; }

        public string Landmarked { get; set; }

        public string AdultEstab { get; set; }

        public string LoftBoard { get; set; }

        public string CityOwned { get; set; }

        public string Littlee { get; set; }

        [Column("PCFiled")]
        public string Pcfiled { get; set; }

        [Column("eFilingFiled")]
        public string EFilingFiled { get; set; }

        public string Plumbing { get; set; }

        public string Mechanical { get; set; }

        public string Boiler { get; set; }

        public string FuelBurning { get; set; }

        public string FuelStorage { get; set; }

        public string Standpipe { get; set; }

        public string Sprinkler { get; set; }

        public string FireAlarm { get; set; }

        public string Equipment { get; set; }

        public string FireSuppression { get; set; }

        public string CurbCut { get; set; }

        public string Other { get; set; }

        public string OtherDescription { get; set; }

        public string ApplicantsFirstName { get; set; }

        public string ApplicantsLastName { get; set; }

        public string ApplicantProfessionalTitle { get; set; }

        public string ApplicantLicenseNum { get; set; }

        public string ProfessionalCert { get; set; }

        public DateTime? PreFilingDate { get; set; }

        public DateTime? Paid { get; set; }

        public DateTime? FullyPaid { get; set; }

        public DateTime? Assigned { get; set; }

        public DateTime? Approved { get; set; }

        public DateTime? FullyPermitted { get; set; }

        public decimal? InitialCost { get; set; }

        public decimal? TotalEstFee { get; set; }

        public string FeeStatus { get; set; }

        public string ExistingZoningSqft { get; set; }

        public string ProposedZoningSqft { get; set; }

        public string HorizontalEnlrgmt { get; set; }

        public string VerticalEnlrgmt { get; set; }

        [Column("EnlargementSQFootage")]
        public string EnlargementSqfootage { get; set; }

        public string StreetFrontage { get; set; }

        public string ExistingNoofStories { get; set; }

        public string ProposedNoofStories { get; set; }

        public string ExistingHeight { get; set; }

        public string ProposedHeight { get; set; }

        public string ExistingDwellingUnits { get; set; }

        public string ProposedDwellingUnits { get; set; }

        public string ExistingOccupancy { get; set; }

        public string ProposedOccupancy { get; set; }

        public string SiteFill { get; set; }

        public string ZoningDist1 { get; set; }

        public string ZoningDist2 { get; set; }

        public string ZoningDist3 { get; set; }

        public string SpecialDistrict1 { get; set; }

        public string SpecialDistrict2 { get; set; }

        public string OwnerType { get; set; }

        public string NonProfit { get; set; }

        public string OwnersFirstName { get; set; }

        public string OwnersLastName { get; set; }

        public string OwnersBusinessName { get; set; }

        public string OwnersHouseNumber { get; set; }

        public string OwnersHouseStreetName { get; set; }

        public string City { get; set; }

        public string State { get; set; }

        public string Zip { get; set; }

        public string OwnersPhone { get; set; }

        public string JobDescription { get; set; }

        [Column("DOBRunDate")]
        public DateTime? DobrunDate { get; set; }

        [Column("JOBS1NO")]
        public string Jobs1no { get; set; }

        [Column("TOTALCONSTRUCTIONFLOORAREA")]
        public string Totalconstructionfloorarea { get; set; }

        [Column("WITHDRAWALFLAG")]
        public string Withdrawalflag { get; set; }

        [Column("SIGNOFFDATE")]
        public DateTime? Signoffdate { get; set; }

        [Column("SPECIALACTIONSTATUS")]
        public string Specialactionstatus { get; set; }

        [Column("SPECIALACTIONDATE")]
        public string Specialactiondate { get; set; }

        [Column("BUILDINGCLASS")]
        public string Buildingclass { get; set; }

        [Column("JOBNOGOODCOUNT")]
        public string Jobnogoodcount { get; set; }

        [Column("GISLATITUDE")]
        public string Gislatitude { get; set; }

        [Column("GISLONGITUDE")]
        public string Gislongitude { get; set; }

        [Column("GISCOUNCILDISTRICT")]
        public string Giscouncildistrict { get; set; }

        [Column("GISCENSUSTRACT")]
        public string Giscensustract { get; set; }

        [Column("GISNTANAME")]
        public string Gisntaname { get; set; }

        [Column("GISBIN")]
        public string Gisbin { get; set; }
    }
}
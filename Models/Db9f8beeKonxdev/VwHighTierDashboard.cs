using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KonXProWebApp.Models.db_9f8bee_konxdev
{
    [Table("vwHighTierDashboard", Schema = "dbo")]
    public partial class VwHighTierDashboard
    {
        public int? JobNum { get; set; }

        public string Borough { get; set; }

        public string HouseNum { get; set; }

        public string Street { get; set; }

        public DateTime? LatestActionDate { get; set; }

        public string ProjectType { get; set; }

        public decimal? EstimatedCost { get; set; }

        public string JobDescription { get; set; }

        public string Neighborhood { get; set; }
    }
}
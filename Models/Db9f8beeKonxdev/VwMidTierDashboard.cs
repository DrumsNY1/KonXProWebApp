using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KonXProWebApp.Models.db_9f8bee_konxdev
{
    // View mapping (ToView) is configured in db_9f8bee_konxdevContext.OnModelCreating,
    // not here — a [Table] attribute here would give this entity a table name too,
    // which makes EF try to CreateTable it as a migration despite ToView().
    public partial class VwMidTierDashboard
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
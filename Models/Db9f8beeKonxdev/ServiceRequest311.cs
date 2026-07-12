using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KonXProWebApp.Models.db_9f8bee_konxdev
{
    [Table("ServiceRequests311", Schema = "dbo")]
    public partial class ServiceRequest311
    {
        [Key]
        public string UniqueKey { get; set; }

        public DateTime? CreatedDate { get; set; }

        public DateTime? ClosedDate { get; set; }

        public string Agency { get; set; }

        public string ComplaintType { get; set; }

        public string Descriptor { get; set; }

        public string IncidentZip { get; set; }

        public string IncidentAddress { get; set; }

        public string Borough { get; set; }

        public string Bbl { get; set; }

        public string Status { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KonXProWebApp.Models.db_9f8bee_konxdev
{
    [Table("vwDemoDisplay", Schema = "dbo")]
    public partial class VwDemoDisplay
    {
        public string Content { get; set; }

        public string Summary { get; set; }

        [Column(TypeName="datetime2")]
        public DateTime? CompletionDate { get; set; }
    }
}
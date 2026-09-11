using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KonXProWebApp.Models.db_9f8bee_konxdev
{
    // View mapping (ToView) is configured in db_9f8bee_konxdevContext.OnModelCreating,
    // not here — a [Table] attribute here would give this entity a table name too,
    // which makes EF try to CreateTable it as a migration despite ToView().
    public partial class VwDemoDisplay
    {
        public string Content { get; set; }

        public string Summary { get; set; }

        [Column(TypeName="datetime2")]
        public DateTime? CompletionDate { get; set; }
    }
}
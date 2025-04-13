using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KonXProWebApp.Models.db_9f8bee_konxdev
{
    [Table("BlogContent", Schema = "dbo")]
    public partial class BlogContent
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("ContentID")]
        public int ContentId { get; set; }

        public string Content { get; set; }

        public string Summary { get; set; }

        [Column(TypeName="datetime2")]
        public DateTime? CompletionDate { get; set; }

        [Column("SourceID")]
        public int? SourceId { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KonXProWebApp.Models.db_9f8bee_konxdev
{
    [Table("BlogFeedSources", Schema = "dbo")]
    public partial class BlogFeedSource
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("FeedID")]
        public int FeedId { get; set; }

        public string FeedName { get; set; }

        public string FeedUrl { get; set; }

        public string FeedCategory { get; set; }
    }
}
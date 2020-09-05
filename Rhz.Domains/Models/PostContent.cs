using Microsoft.Azure.Cosmos.Table;
using System;

namespace Rhz.Domains.Models
{
    public class PostContent : TableEntity
    {
        public string Caption { get; set; }
        public string Preview { get; set; }
        public string Content { get; set; }
        public DateTime PublishedOn { get; set; }
        public DateTime UpdatedOn { get; set; }
        public bool Published { get; set; }
        public string BlobName { get; set; }
    }
}

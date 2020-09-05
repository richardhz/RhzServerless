using Microsoft.Azure.Cosmos.Table;
using System;

namespace Rhz.Domains.Models
{
    public class DisplayContent : TableEntity
    {
        public string HtmlContent { get; set; }
        public DateTime UpdatedOn { get; set; }
        public bool Published { get; set; }
        public string BlobName { get; set; }
    }
}

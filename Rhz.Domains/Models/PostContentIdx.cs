using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.Text;

namespace Rhz.Domains.Models
{
    public class PostContentIdx : TableEntity
    {
        public string DocumentId { get; set; }
        public DateTime PublishedOn { get; set; }
    }
}

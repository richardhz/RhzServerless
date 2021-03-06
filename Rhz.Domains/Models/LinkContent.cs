﻿using System;
using Microsoft.Azure.Cosmos.Table;

namespace Rhz.Domains.Models
{
    public class LinkContent : TableEntity
    {
        public string Caption { get; set; }
        public string Target { get; set; }
        public string Url { get; set; }
        public DateTime UpdatedOn { get; set; }
        public bool Published { get; set; }
    }
}

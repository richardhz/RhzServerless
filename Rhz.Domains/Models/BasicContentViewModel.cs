using System;
using System.Collections.Generic;

namespace Rhz.Domains.Models
{
    [Serializable]
    public class BasicContentViewModel
    {
        public BasicContentViewModel()
        {
            Content = new Dictionary<string, string>();
            Lists = new Dictionary<string, IEnumerable<LinkContentDto>>();
            Documents = new List<PostContentDto>();
        }
        public string RequestPath { get; set; }
        public Dictionary<string, string> Content { get; }
        public Dictionary<string, IEnumerable<LinkContentDto>> Lists { get; }
        public IEnumerable<PostContentDto> Documents { get; set; }

    }
}

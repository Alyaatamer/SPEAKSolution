using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPEAK.Domain.Models.Helpers
{
    public class EmailMessage
    {
        public string? To { get; set; }
        public string? Subject { get; set; }
        public string? Content { get; set; }
        public bool IsHtml { get; set; } = true;
        public List<string> Attachments { get; set; } = new();
    }
}

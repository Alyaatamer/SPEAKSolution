using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPEAK.Domain.Exceptions
{
    public class BadRequestException : Exception
    {
        public List<string> Errors { get; set; } = new();

        public BadRequestException(string message) : base(message)
        {
            Errors = new List<string> { message };
        }

        public BadRequestException(List<string> errors) : base("Validation Failed")
        {
            Errors = errors;
        }

        public BadRequestException(IEnumerable<string> errors) : this(errors.ToList())
        {
        }
    }
}

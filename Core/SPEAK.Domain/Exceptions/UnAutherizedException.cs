using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPEAK.Domain.Exceptions
{
    public class UnAutherizedException : Exception
    {
        public UnAutherizedException() : base("Invalid email or password")
        {
        }

        public UnAutherizedException(string message) : base(message)
        {
        }
    }
}

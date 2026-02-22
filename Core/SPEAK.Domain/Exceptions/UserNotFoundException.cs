using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPEAK.Domain.Exceptions
{
    public class UserNotFoundException(string Email) :NotFoundException($"User with that Email {Email} is not Found")
    {

    }
}

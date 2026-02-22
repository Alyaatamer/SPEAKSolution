using SPEAK.Domain.Models.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPEAK.Abstraction.IServices
{
    public interface IEmailService
    {
        Task SendAsync(EmailMessage emailMessage);
    }
}

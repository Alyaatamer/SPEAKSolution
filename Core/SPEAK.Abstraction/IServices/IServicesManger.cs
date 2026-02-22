using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPEAK.Abstraction.IServices
{
    public interface IServicesManger
    {
        public IAuthenticationServices AuthenticationServices { get; }

        public IEmailService EmailService { get; }
        public IAudioMerger AudioMerger { get; }
    }
}

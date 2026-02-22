using SPEAK.Abstraction.IServices;

namespace SPEAK.Service.Services
{
    public class ServicesManager(IAuthenticationServices authenticationServices , IEmailService emailService , IAudioMerger audioMerger) : IServicesManger
    {
        public IAuthenticationServices AuthenticationServices { get; } = authenticationServices;

        public IEmailService EmailService { get; } = emailService;
        public IAudioMerger AudioMerger { get; } = audioMerger;
    }
}


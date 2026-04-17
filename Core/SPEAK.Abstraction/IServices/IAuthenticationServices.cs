using SPEAK.Shared.DTO_s.IdentityDto;
using System.Security.Claims;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPEAK.Abstraction.IServices
{
    public interface IAuthenticationServices
    {
        Task<UserDto> LoginAsync(LoginDto loginDto);
        Task<UserDto> RegisterAsync(RegisterDto registerDto);
        Task<bool> CheckEmailAsync(string email);
        Task<UserDto> GetCurrentUserAsync(string email);
        Task<UserDto> UpdateParentProfileAsync(string email, UpdateProfileDto dto);
        Task ChangePasswordAsync(string email, ChangePasswordDto dto);


        Task SendForgetPasswordOtpAsync(string email);
        Task<string> VerifyOtpAndGetResetTokenAsync(VerifyOtpDto dto);
        Task ResetPasswordAsync(ResetPasswordDto dto);
        Task<UserDto> GoogleLoginAsync(GoogleLoginDto dto);
        Task<UserDto> CompleteGoogleProfileAsync(ClaimsPrincipal userPrincipal, ChildProfileDto dto);
        Task SendEmailVerificationAsync(string email);
        Task VerifyEmailAsync(VerifyEmailDto dto);
        
        Task SendRegistrationOtpAsync(string email);
        Task<UserDto> VerifyRegistrationOtpAsync(VerifyOtpDto dto);



        Task<UserDto> DoctorRegisterAsync(DoctorRegisterDto dto, string syndicateCardImageUrl, string nationalIdImageUrl, string? vezeetaLink);
    }
}

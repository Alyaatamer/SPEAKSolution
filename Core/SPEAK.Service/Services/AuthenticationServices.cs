using SPEAK.Domain.Exceptions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using SPEAK.Abstraction.IRepositories;
using SPEAK.Abstraction.IServices;
using SPEAK.Domain.Models;
using SPEAK.Domain.Models.Enums;
using SPEAK.Domain.Models.Helpers;
using SPEAK.Domain.Models.Identity;
using SPEAK.Shared.DTO_s.IdentityDto;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json;

namespace SPEAK.Service.Services
{
    public class AuthenticationServices : IAuthenticationServices
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private readonly RedisService _redis;
        private readonly IDoctorRepository _doctorRepository;

        public AuthenticationServices(
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration,
            IEmailService emailService,
            RedisService redis,
            IDoctorRepository doctorRepository)
        {
            _userManager = userManager;
            _configuration = configuration;
            _emailService = emailService;
            _redis = redis;
            _doctorRepository = doctorRepository;
        }

        public async Task<UserDto> LoginAsync(LoginDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email ?? "")
                ?? throw new UserNotFoundException(dto.Email ?? "");

            var isPasswordValid = await _userManager.CheckPasswordAsync(user, dto.Password ?? "");
            if (!isPasswordValid)
                throw new UnAutherizedException("Incorrect password. Please try again.");

            if (user.IsDeleted || user.IsDisabled)
                throw new ForbiddenException("This account has been disabled. Please contact support.");

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "Parent";

            // Doctor must be Approved before getting a token
            string? doctorStatus = null;
            if (roles.Contains("Doctor"))
            {
                var doctorProfile = await _doctorRepository.GetDoctorProfileByUserIdAsync(user.Id);
                doctorStatus = doctorProfile?.Status.ToString();

                if (doctorProfile == null || doctorProfile.Status != DoctorStatus.Approved)
                    throw new ForbiddenException("Your account is pending review. Please wait for admin approval.");
            }

            return new UserDto
            {
                Email = user.Email ?? "",
                DisplayName = user.DisplayName ?? "",
                Token = await CreateTokenAsync(user),
                Role = role,
                DoctorStatus = doctorStatus
            };
        }

        public async Task<UserDto> RegisterAsync(RegisterDto dto)
        {
            var existingUser = await _userManager.FindByEmailAsync(dto.Email ?? "");
            if (existingUser != null)
                throw new BadRequestException(new[] { "Email is already registered." });

            var registrationDataKey = $"reg_data:{(dto.Email ?? "").ToLower()}";
            var registrationData = JsonSerializer.Serialize(dto);
            await _redis.SetAsync(registrationDataKey, registrationData, TimeSpan.FromMinutes(15));

            await SendRegistrationOtpAsync(dto.Email ?? "");

            return new UserDto
            {
                Email = dto.Email ?? "",
                DisplayName = dto.FullName ?? "",
                Token = "",
                Role = "Parent"
            };
        }

        public async Task<bool> CheckEmailAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email ?? "");
            return user != null;
        }

        public async Task<UserDto> GetCurrentUserAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email ?? "")
                ?? throw new UserNotFoundException(email ?? "");

            var roles = await _userManager.GetRolesAsync(user);
            return new UserDto
            {
                Email = user.Email ?? "",
                DisplayName = user.DisplayName ?? "",
                Token = await CreateTokenAsync(user),
                Role = roles.FirstOrDefault() ?? "Parent"
            };
        }

        public async Task SendRegistrationOtpAsync(string email)
        {
            var otp = new Random().Next(100000, 999999).ToString("D6");
            var key = $"reg_otp:{(email ?? "").ToLower()}";
            await _redis.SetAsync(key, otp, TimeSpan.FromMinutes(10));

            var body = $@"
            <div style='font-family:Arial;text-align:center;'>
                <h2>Welcome to SPEAK!</h2>
                <p>Your verification code:</p>
                <h1 style='font-size:48px;color:#007bff;letter-spacing:8px;'>{otp}</h1>
                <p>Expires in 10 minutes.</p>
            </div>";

            await _emailService.SendAsync(new EmailMessage
            {
                To = email ?? "",
                Subject = "Your Registration Verification Code",
                Content = body,
                IsHtml = true
            });
        }

        public async Task<UserDto> VerifyRegistrationOtpAsync(VerifyOtpDto dto)
        {
            var otpKey = $"reg_otp:{(dto.Email ?? "").ToLower()}";
            var storedOtp = await _redis.GetAsync(otpKey);

            if (string.IsNullOrEmpty(storedOtp) || storedOtp != dto.Otp)
                throw new BadRequestException(new[] { "Invalid or expired verification code." });

            var registrationDataKey = $"reg_data:{(dto.Email ?? "").ToLower()}";
            var registrationDataJson = await _redis.GetAsync(registrationDataKey);

            if (string.IsNullOrEmpty(registrationDataJson))
                throw new BadRequestException(new[] { "Registration session expired. Please register again." });

            var registrationData = JsonSerializer.Deserialize<RegisterDto>(registrationDataJson);

            var user = new ApplicationUser
            {
                DisplayName = registrationData?.FullName ?? "",
                Email = registrationData?.Email ?? "",
                UserName = registrationData?.Email ?? "",
                PhoneNumber = registrationData?.PhoneNumber ?? "",
                ChildAge = registrationData?.ChildAge ?? 0,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, registrationData?.Password ?? "");
            if (!result.Succeeded)
                throw new BadRequestException(result.Errors.Select(e => e.Description).ToList());

            await _userManager.AddToRoleAsync(user, "Parent");

            // Create Parent Profile via repository (no direct DbContext reference)
            var parentProfile = new ParentProfile
            {
                UserId = user.Id,
                ChildName = registrationData?.ChildName,
                ChildAge = registrationData?.ChildAge ?? 0,
                ChildGender = registrationData?.ChildGender ?? Gender.Male
            };
            await _doctorRepository.AddParentProfileAsync(parentProfile);

            await _redis.DeleteAsync(otpKey);
            await _redis.DeleteAsync(registrationDataKey);

            return new UserDto
            {
                Email = user.Email ?? "",
                DisplayName = user.DisplayName ?? "",
                Token = await CreateTokenAsync(user),
                Role = "Parent"
            };
        }

        public async Task<UserDto> DoctorRegisterAsync(DoctorRegisterDto dto, string syndicateCardImageUrl, string nationalIdImageUrl, string? vezeetaLink)
        {
            var existingUser = await _userManager.FindByEmailAsync(dto.Email ?? "");
            if (existingUser != null)
                throw new BadRequestException(new[] { "Email is already registered." });

            var user = new ApplicationUser
            {
                DisplayName = dto.FullName ?? "",
                Email = dto.Email ?? "",
                UserName = dto.Email ?? "",
                PhoneNumber = dto.PhoneNumber ?? "",
                EmailConfirmed = true,

            };

            var result = await _userManager.CreateAsync(user, dto.Password ?? "");
            if (!result.Succeeded)
                throw new BadRequestException(result.Errors.Select(e => e.Description).ToList());

            await _userManager.AddToRoleAsync(user, "Doctor");

            // Create Doctor Profile (Pending) via repository
            var doctorProfile = new DoctorProfile
            {
                UserId = user.Id,
                SyndicateCardImageUrl = syndicateCardImageUrl,
                NationalIdImageUrl = nationalIdImageUrl,
                Status = DoctorStatus.Pending,
                VezeetaLink = dto.vezeetaLink
            };
            await _doctorRepository.AddDoctorProfileAsync(doctorProfile);

            return new UserDto
            {
                Email = user.Email ?? "",
                DisplayName = user.DisplayName ?? "",
                Token = "",   // No token — account is Pending
                Role = "Doctor",
                DoctorStatus = "Pending"
            };
        }

        public async Task SendEmailVerificationAsync(string email)
        {
            await SendRegistrationOtpAsync(email);
        }

        public async Task VerifyEmailAsync(VerifyEmailDto dto)
        {
            var key = $"reg_otp:{(dto.Email ?? "").ToLower()}";
            var storedOtp = await _redis.GetAsync(key);

            if (string.IsNullOrEmpty(storedOtp) || storedOtp != dto.Otp)
                throw new BadRequestException(new[] { "Invalid or expired verification code." });

            var user = await _userManager.FindByEmailAsync(dto.Email ?? "")
                ?? throw new UserNotFoundException(dto.Email ?? "");

            user.EmailConfirmed = true;
            await _userManager.UpdateAsync(user);
            await _redis.DeleteAsync(key);
        }

        public async Task<UserDto> GoogleLoginAsync(GoogleLoginDto dto)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadToken(dto.IdToken ?? "") as JwtSecurityToken;

                if (jsonToken == null)
                    throw new BadRequestException(new[] { "Invalid Google ID token" });

                var email = jsonToken.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
                var name = jsonToken.Claims.FirstOrDefault(c => c.Type == "name")?.Value;

                if (string.IsNullOrEmpty(email))
                    throw new BadRequestException(new[] { "Email not found in Google token" });

                var user = await _userManager.FindByEmailAsync(email);

                if (user == null)
                {
                    user = new ApplicationUser
                    {
                        Email = email,
                        UserName = email,
                        DisplayName = name ?? email,
                        EmailConfirmed = true
                    };

                    var result = await _userManager.CreateAsync(user);
                    if (!result.Succeeded)
                        throw new BadRequestException(result.Errors.Select(e => e.Description).ToList());

                    await _userManager.AddToRoleAsync(user, "Parent");
                }

                var roles = await _userManager.GetRolesAsync(user);
                return new UserDto
                {
                    Email = user.Email ?? "",
                    DisplayName = user.DisplayName ?? "",
                    Token = await CreateTokenAsync(user),
                    Role = roles.FirstOrDefault() ?? "Parent"
                };
            }
            catch (Exception ex)
            {
                throw new BadRequestException(new[] { $"Google login failed: {ex.Message}" });
            }
        }

        public async Task SendForgetPasswordOtpAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email ?? "")
                ?? throw new UserNotFoundException(email ?? "");

            var otp = new Random().Next(100000, 999999).ToString("D6");
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);

            var key = $"otp:{(email ?? "").ToLower()}";
            var payload = new { Otp = otp, Token = resetToken };
            var json = JsonSerializer.Serialize(payload);

            await _redis.SetAsync(key, json, TimeSpan.FromMinutes(10));

            var body = $@"
                <div style='font-family:Arial;text-align:center;'>
                    <h2>Password Reset Request</h2>
                    <h1 style='font-size:48px;color:#007bff;letter-spacing:8px;'>{otp}</h1>
                    <p>Expires in 10 minutes.</p>
                </div>";

            await _emailService.SendAsync(new EmailMessage
            {
                To = email ?? "",
                Subject = "Your SPEAK Password Reset Code",
                Content = body,
                IsHtml = true
            });
        }

        public async Task<string> VerifyOtpAndGetResetTokenAsync(VerifyOtpDto dto)
        {
            var key = $"otp:{(dto.Email ?? "").ToLower()}";
            var json = await _redis.GetAsync(key);

            if (string.IsNullOrEmpty(json))
                throw new BadRequestException(new[] { "Invalid or expired verification code." });

            var payload = JsonSerializer.Deserialize<Dictionary<string, string>>(json ?? "") ?? new Dictionary<string, string>();

            if (!payload.TryGetValue("Otp", out var storedOtp) || storedOtp != dto.Otp)
                throw new BadRequestException(new[] { "Invalid or expired verification code." });

            await _redis.DeleteAsync(key);
            return payload["Token"] ?? "";
        }

        public async Task ResetPasswordAsync(ResetPasswordDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email ?? "")
                ?? throw new UserNotFoundException(dto.Email ?? "");

            var result = await _userManager.ResetPasswordAsync(user, dto.Token ?? "", dto.NewPassword ?? "");
            if (!result.Succeeded)
                throw new BadRequestException(result.Errors.Select(e => e.Description).ToList());

            await _emailService.SendAsync(new EmailMessage
            {
                To = dto.Email ?? "",
                Subject = "Your Password Has Been Changed",
                Content = "<h2>Password Updated Successfully</h2><p>Your password was changed.</p>",
                IsHtml = true
            });
        }

        private async Task<string> CreateTokenAsync(ApplicationUser user)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.Email, user.Email ?? ""),
                new(ClaimTypes.Name, user.UserName ?? ""),
                new(ClaimTypes.NameIdentifier, user.Id ?? "")
            };

            var roles = await _userManager.GetRolesAsync(user);
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role ?? "")));

            var keyString = _configuration.GetSection("JWTOptions")["SecurityKey"]
                ?? throw new InvalidOperationException("JWT SecurityKey is missing");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration.GetSection("JWTOptions")["Issuer"] ?? "SPEAK.API",
                audience: _configuration.GetSection("JWTOptions")["Audience"] ?? "SPEAK.Users",
                claims: claims,
                expires: DateTime.Now.AddDays(2),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
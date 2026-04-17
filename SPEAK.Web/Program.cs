using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SPEAK.Abstraction.IServices;
using SPEAK.Abstraction.IRepositories;
using SPEAK.Persistence.Repositories;
using SPEAK.Domain.Models.Helpers;
using SPEAK.Domain.Models.Identity;
using SPEAK.Persistence.Contexts;
using SPEAK.Service.Services;
using SPEAK.Web.Middleware;
using System.Text;
using SPEAK.Web.Hubs;

namespace SPEAK.Web
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers()
                .AddApplicationPart(typeof(SPEAK.Presentation.Controllers.AuthenticationController).Assembly);

            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "SPEAK API", Version = "v1" });

                // Handle conflicting actions (duplicate routes across assemblies)
                c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());

                // Fix duplicate schema name conflicts (classes with same name in different namespaces)
                c.CustomSchemaIds(type => type.FullName?.Replace("+", "."));

                // Add JWT Authentication to Swagger
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });


            #region database
            builder.Services.AddDbContext<UserIdentityDbContext>(options =>
               {
                   options.UseSqlServer(builder.Configuration.GetConnectionString("IdentityConnection"));
               });
            #endregion


            #region Identity
            builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
                {
                    options.Password.RequireDigit = true;
                    options.Password.RequireLowercase = true;
                    options.Password.RequireUppercase = true;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequiredLength = 6;

                    options.User.RequireUniqueEmail = true;
                    options.SignIn.RequireConfirmedEmail = false;
                })
                .AddEntityFrameworkStores<UserIdentityDbContext>()
                .AddDefaultTokenProviders();
            #endregion


            #region JWT
            var jwtOptions = builder.Configuration.GetSection("JWTOptions");
            var securityKey = jwtOptions["SecurityKey"];
            var issuer = jwtOptions["Issuer"];
            var audience = jwtOptions["Audience"];

            if (string.IsNullOrEmpty(securityKey) || string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(audience))
            {
                throw new InvalidOperationException("JWTOptions configuration is missing or incomplete in appsettings.json");
            }

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(securityKey)),
                    ClockSkew = TimeSpan.Zero
                };
            });
            #endregion


            builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
            builder.Services.AddSingleton<RedisService>();
            builder.Services.AddSingleton<IAudioMerger, AudioMerger>();

            #region Services
            builder.Services.AddScoped<IAuthenticationServices, AuthenticationServices>();
            builder.Services.AddScoped<IServicesManger, ServicesManager>();
            builder.Services.AddScoped<IEmailService, EmailService>();
            builder.Services.AddScoped<IDiagnosticRepository, DiagnosticRepository>();
            builder.Services.AddScoped<IAdminService, AdminService>();
            builder.Services.AddScoped<IAdminLogRepository, AdminLogRepository>();
            builder.Services.AddScoped<IDoctorRepository, DoctorRepository>();
            builder.Services.AddHttpClient<IVoiceProcessingService, VoiceProcessingService>();
            builder.Services.AddScoped<SPEAK.Web.Services.IAIService, SPEAK.Web.Services.AIService>();

            builder.Services.AddScoped<IChatRepository, ChatRepository>();
            #endregion

            builder.Services.AddSignalR();


            var app = builder.Build();

            // Seed Roles and Admin Users and apply DB changes
            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<UserIdentityDbContext>();
                context.Database.Migrate();
                
                await SeedData.SeedAsync(scope.ServiceProvider);
            }

            // Configure the HTTP request pipeline.
            app.UseSwagger();
            app.UseSwaggerUI();

            // app.UseHttpsRedirection();

            // Exception Handling Middleware
            app.UseMiddleware<CutomExceptionMiddleware>();

            app.UseAuthentication();
            app.UseAuthorization();
            app.MapHub<ChatHub>("/chatHub");

            app.UseStaticFiles();

            app.MapControllers();

            app.Run();
        }
    }

    /// <summary>
    /// Seeds the 3 roles (Admin, Doctor, Parent) and 4 admin accounts on startup.
    /// Nothing happens if the roles/users already exist — safe to run on every startup.
    /// </summary>
    public static class SeedData
    {
        private static readonly (string Email, string DisplayName)[] AdminUsers =
        {
            ("alyaatamer88@gmail.com",    "Alyaa Tamer"),
            ("Sohermohamed629@gmail.com", "Soher Mohamed"),
            ("engyrefaai@gmail.com",      "Engy Refaai"),
            ("ali.el3a2ad@gmail.com",     "Ali Diyaa")
        };

        private const string AdminPassword = "Speak.team4";

        public static async Task SeedAsync(IServiceProvider services)
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

            // 1. Create Roles 
            string[] roles = { "Admin", "Doctor", "Parent" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // 2. Create Admin Users 
            foreach (var (email, displayName) in AdminUsers)
            {
                var existing = await userManager.FindByEmailAsync(email);
                if (existing == null)
                {
                    var admin = new ApplicationUser
                    {
                        DisplayName = displayName,
                        Email       = email,
                        UserName    = email,
                        EmailConfirmed = true
                    };
                    var result = await userManager.CreateAsync(admin, AdminPassword);
                    if (result.Succeeded)
                        await userManager.AddToRoleAsync(admin, "Admin");
                }
            }
        }
    }
}

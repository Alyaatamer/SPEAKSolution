using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SPEAK.Abstraction.IRepositories;
using SPEAK.Abstraction.IServices;
using SPEAK.Domain.Models.Helpers;
using SPEAK.Domain.Models.Identity;
using SPEAK.Persistence.Contexts;
using SPEAK.Persistence.Repositories;
using SPEAK.Service.Services;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<UserIdentityDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("IdentityConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit           = true;
        options.Password.RequireLowercase       = true;
        options.Password.RequireUppercase       = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength         = 6;
        options.User.RequireUniqueEmail         = true;
        options.SignIn.RequireConfirmedEmail     = false;
    })
    .AddEntityFrameworkStores<UserIdentityDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath        = "/Account/Login";
    options.LogoutPath       = "/Account/Logout";
    options.AccessDeniedPath = "/Account/Login";
    options.ExpireTimeSpan   = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});


builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddSingleton<RedisService>();


builder.Services.AddScoped<IAuthenticationServices, AuthenticationServices>();
builder.Services.AddScoped<IAdminService,           AdminService>();
builder.Services.AddScoped<IEmailService,           EmailService>();
builder.Services.AddScoped<IAdminLogRepository,     AdminLogRepository>();
builder.Services.AddScoped<IDoctorRepository,       DoctorRepository>();


var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // app.UseHsts(); // Disabled for free hosting SSL compatibility
}

// app.UseHttpsRedirection(); // Disabled to allow direct HTTP access
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Default route → HomeController → Index
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

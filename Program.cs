using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WatchWave.Configurations;
using WatchWave.Models;
using WatchWave.Models.GenericRepo;
using WatchWave.Models.Repo;

var builder = WebApplication.CreateBuilder(args);

// Set up AppSettings
builder.Configuration.SetupAppSettings();

var connectionString = AppSettings.ConnectionStrings.DefaultConnection;
var adminDetails = AppSettings.AdminDetails;

// Add services to the container.
//builder.Services.AddScoped<EmailSenderService>();

builder.Services.Configure<SMTPConfig>(builder.Configuration.GetSection("SmtpConfig"));



builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));

builder.Services.Configure<IdentityOptions>(options => options.SignIn.RequireConfirmedEmail = false);
builder.Services.AddIdentity<AppUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<IUserRepo, UserRepo>();
builder.Services.AddScoped<IAPICalls, APICalls>();
builder.Services.AddScoped<IRecordsRepo, RecordsRepo>();
builder.Services.AddScoped(typeof(IGenericRepo<>), typeof(GenericRepo<>));
builder.Services.AddHttpContextAccessor();

// Add controllers with views
builder.Services.AddControllersWithViews();

// Configure application cookie options
builder.Services.ConfigureApplicationCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;
});

var app = builder.Build();

// Apply any pending migrations to the database
using var scope = app.Services.CreateScope();
using var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
await appDbContext.Database.MigrateAsync();

// Ensure that the Admin and User roles exist
using var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
if (!await roleManager.RoleExistsAsync("Admin"))
{
    var adminRole = new IdentityRole("Admin");
    await roleManager.CreateAsync(adminRole);
}

if (!await roleManager.RoleExistsAsync("User"))
{
    var userRole = new IdentityRole("User");
    await roleManager.CreateAsync(userRole);
}

// Ensure the Admin user is created if it doesn't already exist
var adminPassword = adminDetails.Password;
var adminUsername = adminDetails.UserName;
var adminEmail = adminDetails.Email;
var adminProfilePicturePath = adminDetails.ProfilePicturePath;

using var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
var adminUser = await userManager.FindByNameAsync(adminUsername);
if (adminUser == null)
{
    adminUser = new AppUser
    {
        UserName = adminUsername,
        Email = adminEmail,
        EmailConfirmed = true,
        LockoutEnabled = false,
        ProfilePicturePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "ProfilePictures", adminProfilePicturePath)
    };

    var result = await userManager.CreateAsync(adminUser, adminPassword);
    if (result.Succeeded)
    {
        await userManager.AddToRoleAsync(adminUser, "Admin");
    }
}

// Optionally reset the Admin password (if needed)
if (adminUser != null)
{
    var token = await userManager.GeneratePasswordResetTokenAsync(adminUser);
    var resetResult = await userManager.ResetPasswordAsync(adminUser, token, "Admin@1234");

    if (resetResult.Succeeded)
    {
        Console.WriteLine("✅ Admin Password Reset Successfully!");
    }
    else
    {
        Console.WriteLine("❌ Error: " + string.Join(", ", resetResult.Errors.Select(e => e.Description)));
    }
}
else
{
    Console.WriteLine("❌ Admin User Not Found!");
}

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=LandingPage}/{action=LandingPage}/{id?}");

app.Run();

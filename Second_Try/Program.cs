using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using QuestPDF.Infrastructure;
using Second_Try.Data;
using Second_Try.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

// Configure QuestPDF community license (free for open-source / internal use)
QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// ── MVC + Views ───────────────────────────────────────────
builder.Services.AddControllersWithViews();

// ── Database ──────────────────────────────────────────────
builder.Services.AddDbContext<Second_Try.Data.ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── Authentication: Cookie + Google OAuth ─────────────────
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath       = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/Login";
        options.ExpireTimeSpan  = TimeSpan.FromDays(30);
        options.Events = new CookieAuthenticationEvents
        {
            OnValidatePrincipal = async context =>
            {
                if (context.Principal == null) return;
                
                var email = context.Principal.FindFirstValue(ClaimTypes.Email);
                if (string.IsNullOrEmpty(email)) return;

                if (context.Principal.IsInRole("Admin") || context.Principal.IsInRole("Standard"))
                {
                    var dbContext = context.HttpContext.RequestServices.GetRequiredService<Second_Try.Data.ApplicationDbContext>();
                    var user = await dbContext.Employees.FirstOrDefaultAsync(e => e.Email == email);
                    
                    if (user == null || !user.IsActive)
                    {
                        context.RejectPrincipal();
                        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    }
                }
            }
        };
    })
    .AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
    {
        options.ClientId     = builder.Configuration["Authentication:Google:ClientId"]!;
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
        options.CallbackPath = "/signin-google"; // must match Google Console redirect URI
    });

// ── Application Services ──────────────────────────────────
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddHostedService<RequestExpiryService>();
builder.Services.AddScoped<TicketPdfService>();

// ── AI Chat Service ───────────────────────────────────────
builder.Services.AddHttpClient();
builder.Services.AddScoped<IGeminiService, GeminiService>();

// ── Session (for chat history) ────────────────────────────
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout        = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly    = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// ── Run DB seeder on startup ──────────────────────────────
await DbSeeder.SeedAsync(app.Services);

// ── Pipeline ──────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseSession();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

// ── API routes (FareController) ───────────────────────────
app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

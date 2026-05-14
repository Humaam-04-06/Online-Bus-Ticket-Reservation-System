using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Second_Try.Data;
using Second_Try.Services;

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

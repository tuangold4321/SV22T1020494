using Microsoft.AspNetCore.Authentication.Cookies;
using SV22T1020494.BusinessLayers;
using Microsoft.Extensions.FileProviders;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// 1. Configure Session for Shopping Cart
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 2. Configure Cookie Authentication for User Login
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
    });

// 3. Initialize BusinessLayers Configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
Configuration.Initialize(connectionString);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
// Serve static files from this project's wwwroot
app.UseStaticFiles();

// Also serve product images from the Admin project's wwwroot/images/products folder
// so that shop can reference them under the path '/images/products/{fileName}'.
var adminProductsPath = Path.Combine(app.Environment.ContentRootPath, "..", "SV22T1020494.Admin", "wwwroot", "images", "products");
if (Directory.Exists(adminProductsPath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(adminProductsPath),
        RequestPath = "/images/products"
    });
}

app.UseRouting();

// 4. Add Session and Auth Middleware (Order matters!)
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
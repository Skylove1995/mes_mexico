using MMES.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;

using MMES.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("settings.json", optional: false, reloadOnChange: true);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(9999);
});

builder.Services.AddControllersWithViews();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "MMES.Auth";
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

var mysqlConnectionString = builder.Configuration["ConnectionStrings:DefaultConnection:RawConnectionString"]
    ?? throw new InvalidOperationException("Thieu ConnectionStrings:DefaultConnection:RawConnectionString trong settings.json");

builder.Services.AddDbContext<MMesDbContext>(options =>
    options.UseMySql(mysqlConnectionString, new MySqlServerVersion(new Version(8, 0, 0))));

builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
builder.Services.AddScoped<IDmsService, DmsService>();
builder.Services.AddScoped<IAppUpdateService, AppUpdateService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

var staticFileTypes = new FileExtensionContentTypeProvider();
staticFileTypes.Mappings[".avif"] = "image/avif";
staticFileTypes.Mappings[".splinecode"] = "application/octet-stream";

app.UseHttpsRedirection();
app.UseStaticFiles(new StaticFileOptions { ContentTypeProvider = staticFileTypes });

app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

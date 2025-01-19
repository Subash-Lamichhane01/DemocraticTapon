using Microsoft.EntityFrameworkCore;
using DemocraticTapON.Data;
using System.Configuration;
using Microsoft.AspNetCore.Authentication.Cookies;
using DemocraticTapON.Utilities;


var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
Console.WriteLine("Connection String: " + connectionString);

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<EmailService>();

// Add controllers with views
builder.Services.AddControllersWithViews();
// In the ConfigureServices method
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    });

// In the Configure method (after app.UseRouting())



var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}




app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

app.UseRouting();


// Custom route for AboutUs (if using "Index" for the action name)
app.MapControllerRoute(
    name: "about-us",
    pattern: "about-us",  // Matches the URL /about-us
    defaults: new { controller = "Home", action = "Index" }
);

// Default route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

app.Run();

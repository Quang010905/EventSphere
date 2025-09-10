using EventSphere.Models.entities;
using EventSphere.Models.Repositories;
using EventSphere.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Đăng ký DbContext
builder.Services.AddDbContext<EventSphereContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddControllersWithViews();

// Repository pattern
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<AttendanceRepository>();
builder.Services.AddScoped<FeedbackRepository>();
builder.Services.AddScoped<CertificateRepository>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Nếu có login thì bật Authentication
// app.UseAuthentication();
app.UseAuthorization();

// Area route
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

// Default route (mặc định mở Admin/Attendance/Index)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Attendance}/{action=Index}/{id?}",
    defaults: new { area = "Admin" });

app.Run();

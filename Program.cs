using EventSphere.Models.ModelViews;
using EventSphere.Models.entities;
using EventSphere.Models.Repositories;
using EventSphere.Repositories;
using EventSphere.Service.Email;
using EventSphere.Service.Otp;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSession();
builder.Services.AddMemoryCache();

// Otp configuration
builder.Services.Configure<OtpSettings>(builder.Configuration.GetSection("OtpSettings"));
builder.Services.AddSingleton<IOtpService, OtpService>();
builder.Services.AddScoped<IOtpService, OtpService>();

// Email configuration
builder.Services.AddScoped<IEmailSender, EmailSender>();
builder.Services.Configure<MailSettings>(builder.Configuration.GetSection("MailSettings"));

// DbContext
builder.Services.AddDbContext<EventSphereContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repository pattern
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<AttendanceRepository>();
builder.Services.AddScoped<FeedbackRepository>();
builder.Services.AddScoped<CertificateRepository>();
builder.Services.AddScoped<EventSeatingRepository>();
builder.Services.AddScoped<EventShareLogRepository>();
builder.Services.AddScoped<HomeRepository>();


var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

// Area routes
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Client}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "client_default",
    pattern: "{controller=Home}/{action=Index}/{id?}",
    defaults: new { area = "Client" });

//app.MapControllerRoute(
//    name: "admin_default",
//    pattern: "{controller=Attendance}/{action=Index}/{id?}",
//    defaults: new { area = "Admin" });

app.Run();

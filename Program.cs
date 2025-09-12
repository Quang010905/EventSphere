using EventSphere.Models.ModelViews;
using EventSphere.Models.entities;
using EventSphere.Models.Repositories;
using EventSphere.Repositories;
using EventSphere.Service.Email;
using EventSphere.Service.Otp;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSession();
builder.Services.AddMemoryCache();

// OTP
builder.Services.Configure<OtpSettings>(builder.Configuration.GetSection("OtpSettings"));
builder.Services.AddScoped<IOtpService, OtpService>();

// Email
builder.Services.Configure<MailSettings>(builder.Configuration.GetSection("MailSettings"));
builder.Services.AddTransient<IEmailSender, EmailSender>();

// DbContext (Scoped mặc định)
builder.Services.AddDbContext<EventSphereContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repository pattern
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<AttendanceRepository>();
builder.Services.AddScoped<FeedbackRepository>();
builder.Services.AddScoped<CertificateRepository>();
builder.Services.AddScoped<EventSeatingRepository>();
builder.Services.AddScoped<EventShareLogRepository>();
builder.Services.AddScoped<UserRepositoryEf>();
builder.Services.AddScoped<HomeRepository>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseStaticFiles();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

// Map Area Routes
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Client}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "client_default",
    pattern: "{controller=Home}/{action=Index}/{id?}",
    defaults: new { area = "Client" });

app.MapControllerRoute(
    name: "organizer_default",
    pattern: "{controller=ORegistration}/{action=Index}/{id?}",
    defaults: new { area = "Organizer" });

//app.MapControllerRoute(
//    name: "admin_default",
//    pattern: "{controller=Attendance}/{action=Index}/{id?}",
//    defaults: new { area = "Admin" });

app.Run();

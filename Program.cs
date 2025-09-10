
﻿using EventSphere.Models.entities;
using EventSphere.Models.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();


// Đăng ký DbContext
builder.Services.AddDbContext<EventSphereContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


builder.Services.AddDbContext<EventSphereContext>(options =>
{
    options.UseSqlServer(connectionString);
});

builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

builder.Services.AddScoped<AttendanceRepository>();


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

app.MapStaticAssets();

app.UseRouting();
app.UseAuthorization();

// Area route (chuẩn, controller mặc định = Home trong area nếu có)
app.MapControllerRoute(
    name: "areas",

    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

// Default route: nếu muốn trang root mở luôn Admin/Attendance/Index, set defaults tương ứng
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Attendance}/{action=Index}/{id?}",
    defaults: new { area = "Admin" })

    .WithStaticAssets();


app.Run();

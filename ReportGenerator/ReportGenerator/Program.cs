using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using ReportGenerator.Database.DbControllers;
using ReportGenerator.Models;
using ReportGenerator.Services;
using ReportGenerator.Services.Crypt;

namespace ReportGenerator
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Конфигурация
            var config = new NotificationConfig
            {
                CpuThresholdPercent = builder.Configuration.GetValue<double>("Monitoring:CpuThreshold", 80),
                MemoryThresholdBytes = builder.Configuration.GetValue<long>("Monitoring:MemoryThreshold", 1024 * 1024 * 1024),
                EnableEmailNotifications = builder.Configuration.GetValue<bool>("Monitoring:EnableEmail", false),
                EmailRecipients = builder.Configuration.GetSection("Monitoring:EmailRecipients").Get<List<string>>() ?? new()
            };

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            builder.Services.AddSingleton(config);
            builder.Services.AddSingleton<NotificationService>();
            builder.Services.AddSingleton<NotificationConfig>();
            builder.Services.AddSingleton<PerformanceMonitorService>();

            //builder.Services.AddDbContext<ApplicationDbContext>(options =>
            //    options.UseMySQL(builder.Configuration.GetConnectionString("DefaultConnection")));

            var app = builder.Build();          

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();

            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();

            app.Run();
        }
    }
}

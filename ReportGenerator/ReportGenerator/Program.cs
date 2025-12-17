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

            // MVC
            builder.Services.AddControllersWithViews();

            builder.Services.AddSingleton(sp =>
                builder.Configuration.GetSection("Glpi").Get<ReportGenerator.Models.GlpiOptions>()
                ?? new ReportGenerator.Models.GlpiOptions());

            builder.Services.AddHttpClient<ReportGenerator.Services.GlpiClient>(c =>
            {
                c.Timeout = TimeSpan.FromSeconds(30);
            });

            builder.Services.AddSingleton<ReportGenerator.Services.GlpiIncidentService>();

            // оставь только FileWatcherService (EventLogListenerService убран)
            builder.Services.AddHostedService<ReportGenerator.Services.FileWatcherService>();


            // Конфигурация мониторинга (ОДИН экземпляр)
            var monitoringConfig = new NotificationConfig
            {
                CpuThresholdPercent = builder.Configuration.GetValue<double>("Monitoring:CpuThreshold", 80),
                MemoryThresholdBytes = builder.Configuration.GetValue<long>("Monitoring:MemoryThreshold", 1024L * 1024 * 1024),
                EnableEmailNotifications = builder.Configuration.GetValue<bool>("Monitoring:EnableEmail", false),
                EmailRecipients = builder.Configuration.GetSection("Monitoring:EmailRecipients").Get<List<string>>() ?? new List<string>()
            };

            builder.Services.AddSingleton(monitoringConfig);

            // Твои сервисы
            builder.Services.AddSingleton<NotificationService>();
            builder.Services.AddSingleton<RuntimeMetricsListener>();
            builder.Services.AddSingleton<MetricsService>();

            // PerformanceMonitorService у тебя BackgroundService -> запускаем как hosted service,
            // но оставляем ОДИН экземпляр и для DI.
            builder.Services.AddSingleton<PerformanceMonitorService>();
            builder.Services.AddHostedService(sp => sp.GetRequiredService<PerformanceMonitorService>());

            // Слушатель файла events.log (hosted service)
            builder.Services.AddSingleton<FileWatcherService>();
            builder.Services.AddHostedService(sp => sp.GetRequiredService<FileWatcherService>());

            // В Docker/WSL2 EventLog слушать нельзя — не регистрируем.
            // builder.Services.AddHostedService<EventLogListenerService>();

            builder.Services.AddSingleton(sp =>
            {
                string baseUrl = Environment.GetEnvironmentVariable("GLPI_BASE_URL") ?? "http://host.docker.internal/glpi/apirest.php/";
                string appToken = Environment.GetEnvironmentVariable("GLPI_APP_TOKEN") ?? "UnGr7V5EKqqu2qel7XUmCu5gqMtVU41kCpVUwx2U";
                string userToken = Environment.GetEnvironmentVariable("GLPI_USER_TOKEN") ?? "4G0d02SkzBbZ663EzZGDSLy5ztGQWpmI5D5ewo9u";
                string logPath = Environment.GetEnvironmentVariable("LOG_FILE_PATH") ?? "/app/Scripts/events.log";

                var opt = new GlpiOptions
                {
                    BaseUrl = baseUrl,
                    AppToken = appToken,
                    UserToken = userToken,
                    CategoryId = int.Parse(Environment.GetEnvironmentVariable("GLPI_CATEGORY_ID") ?? "15"),
                    RecipientUserId = int.Parse(Environment.GetEnvironmentVariable("GLPI_RECIPIENT_USER_ID") ?? "6"),
                };

                return opt;
            });

            builder.Services.AddHttpClient<GlpiClient>(c =>
            {
                c.Timeout = TimeSpan.FromSeconds(30);
            });

            builder.Services.AddSingleton<GlpiIncidentService>();
            builder.Services.AddHostedService<FileWatcherService>();

            // ВАЖНО: EventLogListenerService НЕ регистрируем
            // builder.Services.AddHostedService<EventLogListenerService>();

            var app = builder.Build();

            // Pipeline
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
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

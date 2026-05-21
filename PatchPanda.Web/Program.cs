using PatchPanda.Web.Components;
using PatchPanda.Web.Services.Background;

namespace PatchPanda.Web;

internal sealed partial class Program
{
    private static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddRazorComponents().AddInteractiveServerComponents();
        builder.Services.AddHttpClient();
        builder.Services.AddControllers();
        builder.Services.AddSingleton<DockerService>();

        builder.Services.AddSingleton<IPortainerService>(sp =>
            new PortainerService(
                sp.GetRequiredService<IConfiguration>(),
                sp.GetRequiredService<ILogger<PortainerService>>()
            ));

        builder.Services.AddSingleton<IVersionService, VersionService>();

        builder.Services.AddSingleton<IDiscordService>(sp =>
            new DiscordService(
                sp.GetRequiredService<IConfiguration>(),
                sp.GetRequiredService<ILogger<DiscordService>>()
            ));

        builder.Services.AddSingleton<IAppriseService, AppriseService>();

        builder.Services.AddSingleton<INotificationService>(sp =>
            new NotificationService(
                sp.GetRequiredService<IDiscordService>(),
                sp.GetRequiredService<IAppriseService>(),
                sp.GetRequiredService<ILogger<NotificationService>>()
            ));

        builder.Services.AddSingleton<IAiService, OllamaService>();

        builder.Services.AddSingleton<UpdateService>(sp =>
            new UpdateService(
                sp.GetRequiredService<DockerService>(),
                sp.GetRequiredService<IDbContextFactory<DataContext>>(),
                sp.GetRequiredService<IFileService>(),
                sp.GetRequiredService<ILogger<UpdateService>>(),
                sp.GetRequiredService<IPortainerService>(),
                sp.GetRequiredService<IVersionService>(),
                sp.GetRequiredService<JobRegistry>(),
                sp.GetRequiredService<INotificationService>()
            ));

        builder.Services.AddSingleton<IFileService, SystemFileService>();
        builder.Services.AddSingleton<JobRegistry>();
        builder.Services.AddSingleton<JobQueue>();
        builder.Services.AddHostedService<VersionCheckHostedService>();
        builder.Services.AddHostedService<UpdateBackgroundService>();

        var baseUrl = builder.Configuration.GetValue<string?>(VariableKeys.BaseUrl);

        Constants.BaseUrl = baseUrl?.TrimEnd('/');

#if DEBUG
        builder.Services.AddDbContextFactory<DataContext>(CreateDebugDatabaseAtWorkingFolder);
#else
        builder.Services.AddDbContextFactory<DataContext>(CreateDatabaseAtRoot);
#endif

        var app = builder.Build();

        var dbContext = await app
            .Services.GetRequiredService<IDbContextFactory<DataContext>>()
            .CreateDbContextAsync();

        if (dbContext.Database.IsRelational())
            await dbContext.Database.MigrateAsync();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }
        app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
        app.UseHttpsRedirection();

        app.UseAntiforgery();
        app.MapStaticAssets();
        app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

        app.MapControllers();

        await ValidatePortainerAccessToken(app.Services.GetRequiredService<IPortainerService>());

        await app.RunAsync();
    }

    private static void CreateDebugDatabaseAtWorkingFolder(DbContextOptionsBuilder opt)
    {
        opt.UseSqlite($"Data Source={Constants.DbName}");
        opt.EnableSensitiveDataLogging();
    }

    private static void CreateDatabaseAtRoot(DbContextOptionsBuilder opt)
    {
        Directory.CreateDirectory("/app/data");
        opt.UseSqlite($"Data Source=/app/data/{Constants.DbName}");
    }

    private static async Task ValidatePortainerAccessToken(IPortainerService portainerService)
    {
        if (!portainerService.IsConfigured || !portainerService.IsAccessTokenConfigured)
            return;

        await portainerService.ValidateAccessTokenAsync();
    }
}

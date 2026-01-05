using AspireDashboardBI.ServiceDefaults;
using AspireDashboardBI.Web;
using AspireDashboardBI.Web.Components;
using AspireDashboardBI.Web.Configuration;
using DevExpress.DashboardAspNetCore;
using DevExpress.DashboardCommon;
using DevExpress.DashboardWeb;
using DevExpress.DataAccess.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

IFileProvider fileProvider = builder.Environment.ContentRootFileProvider;

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();
builder.AddRedisOutputCache("cache");

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpClient<WeatherApiClient>(client =>
{
    // This URL uses "https+http://" to indicate HTTPS is preferred over HTTP.
    // Learn more about service discovery scheme resolution at https://aka.ms/dotnet/sdschemes.
    client.BaseAddress = new("https+http://apiservice");
});

builder.AddAzureNpgsqlDbContext<AppDbContext>("sample", null,
    optionsBuilder =>
    {
        optionsBuilder.UseNpgsql(npgsqlBuilder =>
        {
            npgsqlBuilder.MigrationsHistoryTable("__EFMigrationsHistory", "sample");
        })
            .UseSnakeCaseNamingConvention();
    });

// Ensure Identity is registered so we can seed ApplicationUser via UserManager
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// Add controllers required by Dashboard controller
builder.Services.AddControllers();

builder.Services.AddScoped<DashboardConfigurator>((IServiceProvider serviceProvider) =>
{
    DashboardConfigurator configurator = new DashboardConfigurator();
    configurator.SetDashboardStorage(new DashboardFileStorage(fileProvider.GetFileInfo("Data/Dashboards").PhysicalPath));

    // Create a sample JSON data source
    DataSourceInMemoryStorage dataSourceStorage = new DataSourceInMemoryStorage();
    DashboardJsonDataSource jsonDataSourceUrl = new DashboardJsonDataSource("JSON Data Source (URL)");
    jsonDataSourceUrl.JsonSource = new UriJsonSource(
        new Uri("https://raw.githubusercontent.com/DevExpress-Examples/DataSources/master/JSON/customers.json"));
    jsonDataSourceUrl.RootElement = "Customers";
    dataSourceStorage.RegisterDataSource("jsonDataSourceUrl", jsonDataSourceUrl.SaveToXml());

    // Register EF (or SQL fallback) data source
    EFDataSourceConfigurator.ConfigureDataSource(dataSourceStorage);

    configurator.SetDataSourceStorage(dataSourceStorage);

    return configurator;
});

var app = builder.Build();

// Run migrations and seed DB on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var db = services.GetRequiredService<AppDbContext>();
        // Apply migrations
        await db.Database.MigrateAsync();

        // Seed user via UserManager (if available)
        var userManager = services.GetService<UserManager<ApplicationUser>>();
        if (userManager != null)
        {
            const string adminUserName = "admin";
            const string adminEmail = "admin@example.com";
            const string adminPassword = "P@ssw0rd!";

            if (await userManager.FindByNameAsync(adminUserName) == null)
            {
                var admin = new ApplicationUser
                {
                    UserName = adminUserName,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FirstName = "Admin",
                    LastName = "User"
                };

                var createResult = await userManager.CreateAsync(admin, adminPassword);
                if (!createResult.Succeeded)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogWarning("Failed to create admin user: {Errors}", string.Join(", ", createResult.Errors.Select(e => e.Description)));
                }
            }
        }

        // Seed Orders (AppDbContext.Customers) - add multiple sample orders
        if (!await db.Customers.AnyAsync())
        {
            var orders = new[]
            {
                new Order { CustomerName = "Acme Corp" },
                new Order { CustomerName = "Contoso Ltd" },
                new Order { CustomerName = "Sample Customer" }
            };

            db.Customers.AddRange(orders);
            await db.SaveChangesAsync();
        }
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred applying migrations or seeding the database.");
        throw;
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapDashboardRoute("api/dashboard", "DefaultDashboard");

// Map controller routes
app.MapControllers();

app.UseOutputCache();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.Run();
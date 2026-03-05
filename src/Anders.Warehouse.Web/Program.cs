using Anders.Warehouse.Web.Data;
using Anders.Warehouse.Web.Domain;
using Anders.Warehouse.Web.Infrastructure;
using Anders.Warehouse.Web.Interfaces;
using Anders.Warehouse.Web.Jobs;
using Anders.Warehouse.Web.Options;
using Anders.Warehouse.Web.Services;
using FluentValidation;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, lc) => lc.ReadFrom.Configuration(ctx.Configuration).WriteTo.Console());

builder.Services.Configure<GoogleSearchOptions>(builder.Configuration.GetSection("GoogleSearch"));
builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection("Storage"));

var configuredProvider = builder.Configuration.GetValue<string>("DatabaseProvider") ?? "Sqlite";
var forceSqlServerInDevelopment = builder.Configuration.GetValue<bool>("Database:ForceSqlServerInDevelopment");
var enableSqlServer = builder.Configuration.GetValue<bool>("Database:EnableSqlServer");

var useSqlServer = configuredProvider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase) && enableSqlServer;
if (builder.Environment.IsDevelopment() && !forceSqlServerInDevelopment)
{
    useSqlServer = false;
}

var dbProvider = useSqlServer ? "SqlServer" : "Sqlite";
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=warehouse-dev.db";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (dbProvider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlServer(connectionString);
    }
    else
    {
        options.UseSqlite(connectionString);
    }
});

builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

var isSqlServer = useSqlServer;
var enableHangfire = builder.Configuration.GetValue<bool?>("Hangfire:Enabled") ?? isSqlServer;

if (enableHangfire)
{
    builder.Services.AddHangfire(config => config.UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
        {
            PrepareSchemaIfNecessary = true
        }));
    builder.Services.AddHangfireServer();
}

builder.Services.AddScoped<ITenantProvider, HttpTenantProvider>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IPdfParserPipeline, PdfParserPipeline>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IAgreementValidationService, AgreementValidationService>();
builder.Services.AddScoped<IProductImageService, GoogleProductImageService>();
builder.Services.AddScoped<IPriceMonitorService, PriceMonitorService>();
builder.Services.AddScoped<ISecretStore, UserSecretsSecretStore>();
builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();

builder.Services.AddScoped<IPriceSource, ApiPriceSource>();
builder.Services.AddScoped<IPriceSource, ScrapePriceSource>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    if (isSqlServer)
    {
        await db.Database.MigrateAsync();
    }
    else
    {
        db.Database.EnsureCreated();
    }

    await SeedData.InitializeAsync(scope.ServiceProvider);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

if (enableHangfire)
{
    app.UseHangfireDashboard("/hangfire");
    RecurringJob.AddOrUpdate<WeeklyPriceMonitorJob>("weekly-price-monitor", x => x.RunAsync(), Cron.Weekly);
}

app.MapRazorPages();
app.MapControllers();

app.Run();

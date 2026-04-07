using InsaDemoRealtimePrices.Hubs;
using InsaDemoRealtimePrices.Options;
using InsaDemoRealtimePrices.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSignalR();

builder.Services.Configure<EodPriceFeedOptions>(builder.Configuration.GetSection(EodPriceFeedOptions.SectionName));
builder.Services.AddSingleton<PriceSnapshotStore>();
builder.Services.AddSingleton<SqlServerPriceRepository>();
builder.Services.AddHostedService<EodPriceFeedBackgroundService>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .AllowAnyHeader()
            .AllowAnyMethod()
            .SetIsOriginAllowed(_ => true)
            .AllowCredentials();
    });
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors();
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapControllers();
app.MapHub<PricesHub>("/hubs/prices");
app.MapGet("/", () => Results.Redirect("/index.html"));
app.MapFallbackToFile("index.html");

app.Run();

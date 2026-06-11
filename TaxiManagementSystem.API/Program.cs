using Scalar.AspNetCore;
using TaxiManagementSystem.API.Notifier;
using TaxiManagementSystem.API.Repository;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddSingleton<EventNotifier>();
builder.Services.AddScoped<ITMSRepository, TMSRepository>();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// ポートの設定
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.MapScalarApiReference(
        endpointPrefix:"/api-docs",
        configureOptions: options =>
        {
            options
                .WithTitle("Title")
                .WithTheme(ScalarTheme.BluePlanet);
        });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

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

// オリジンの許可
builder.Services.AddCors(options =>
{
    options.AddPolicy("DebugCors", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5502",
                "http://127.0.0.1:5502"
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
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

app.UseCors("DebugCors");

app.UseDefaultFiles(); // index.htmlを自動表示
app.UseStaticFiles();

app.MapControllers();

app.Run();

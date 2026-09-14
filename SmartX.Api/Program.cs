using SmartX.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddSingleton<SensorStore>();
builder.Services.AddSingleton<TelemetryStore>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("SmartXClient", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors("SmartXClient");

app.MapControllers();

app.Run();
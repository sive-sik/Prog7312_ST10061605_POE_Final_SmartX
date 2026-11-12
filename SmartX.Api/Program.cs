using SmartX.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddSingleton<SensorStore>();
builder.Services.AddSingleton<TelemetryStore>();
builder.Services.AddSingleton<AttachmentStore>();
builder.Services.AddSingleton<RealTimeProcessingStore>();
builder.Services.AddHostedService<TelemetryQueueWorker>();
builder.Services.AddSingleton<MeshNetworkStore>();

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

using AzureServiceBusSample.Endpoints;
using AzureServiceBusSample.Options;
using AzureServiceBusSample.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSingleton<IServiceBusService, ServiceBusService>();
builder.Services.Configure<AzureOptions>(builder.Configuration.GetSection(AzureOptions.Azure));


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.AddAzureServiceBusEndpoints();

app.Run();

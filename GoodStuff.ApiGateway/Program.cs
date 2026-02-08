using GoodStuff.ApiGateway.Extensions;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.AddGatewayConfiguration();
builder.Services.AddGatewayServices(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.UseGatewayPipeline();
await app.UseOcelot();
app.Run();

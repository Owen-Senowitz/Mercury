using Mercury.Api.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMercuryOptions();

var app = builder.Build();

app.UseHttpsRedirection();

app.Run();

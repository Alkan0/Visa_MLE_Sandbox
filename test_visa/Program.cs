using Microsoft.OpenApi;
using test_visa.Configurations;
using test_visa.Services;
using static test_visa.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
object value = builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Visa MLE Tool", Version = "v1" });
});

builder.Services.Configure<MleOptions>(builder.Configuration.GetSection("Mle"));
builder.Services.AddSingleton<VisaMleService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();
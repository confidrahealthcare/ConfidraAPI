using ConfidraApi.Business;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options => options.AddPolicy("Frontend", policy => policy.WithOrigins("http://localhost:5173").AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddBusinessServices(builder.Configuration);

var app = builder.Build();

// Enable Swagger UI unconditionally for local testing. Adjust or guard with configuration in production.
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "ConfidraApi v1");
});

app.UseHttpsRedirection();
app.UseCors("Frontend");
app.MapControllers();

app.Run();
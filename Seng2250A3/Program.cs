using Seng2250A3.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddScoped<IMailjetService, MailjetService>();

builder.Services.AddControllers();

// Add services to the container.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
});

// Register the service with the interface

// Configure Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Allow HTTP requests
app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

app.Run();


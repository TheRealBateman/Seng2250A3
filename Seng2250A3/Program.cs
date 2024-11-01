using Seng2250A3.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IMailjetService, MailjetService>();
builder.Services.AddSingleton<IUserService, UserService>();
builder.Services.AddHostedService<StartupInitializationService>();
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
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();
var mailjetService = new MailjetService(configuration);
await mailjetService.SendVerificationEmailAsync("batesysgaming@gmail.com", "123456");
app.UseHttpsRedirection();

// Allow HTTP requests
app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

app.Run();


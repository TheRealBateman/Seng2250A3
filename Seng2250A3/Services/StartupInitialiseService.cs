namespace Seng2250A3.Services;

public class StartupInitializationService : IHostedService
{
    private readonly IUserService _userService;

    public StartupInitializationService(IUserService userService)
    {
        _userService = userService;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Initialize the default user here using IUserService
        _userService.InitializeDefaultUser();
        return Task.CompletedTask;
    }


    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
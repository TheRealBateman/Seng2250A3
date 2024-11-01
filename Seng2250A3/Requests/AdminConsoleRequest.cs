using Seng2250A3.Enums;

namespace Seng2250A3.Requests;

public class AdminConsoleRequest
{
    public CommandType Command { get; set; }
    public string Username { get; set; }
    public string? Email { get; set; }
    public bool? IsAdmin { get; set; }
    public SecurityLevel? SecurityLevel { get; set; }
}
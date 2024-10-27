using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Mvc;
using Seng2250A3.Services;

namespace Seng2250A3.Controllers;


[ApiController]
[Route("[controller]")]
public class UserController : Controller 
{
    private static Dictionary<string, User> _users = new()
    {
        {
            "root",
            new User
            {
                Username = "root", Email = "root@example.com", Password = GenerateRandomPassword(), IsAdmin = true
            }
        }
    };

    private readonly IMailjetService _mailjetService;
    private static readonly Dictionary<string, string> VerificationCodes = new();
    private static readonly Dictionary<string, (string Token, DateTime Expiry)> Tokens = new();
    private readonly ILogger<UserController> _logger;
    public UserController(IMailjetService mailjetService, ILogger<UserController> logger)
    {
        _mailjetService = mailjetService;
        _logger = logger;
        _logger.LogInformation("UserController instantiated");
        InitializeDefaultUser();
    }
    private void InitializeDefaultUser()
    {
        string randomPassword = GenerateRandomPassword();
        byte[] salt = RandomNumberGenerator.GetBytes(128 / 8);
        string hashedPassword = Convert.ToBase64String(KeyDerivation.Pbkdf2(
            password: randomPassword,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 100000,
            numBytesRequested: 256 / 8));

        string storedPassword = $"{Convert.ToBase64String(salt)}:{hashedPassword}";

        _users["root"] = new User
        {
            Username = "root",
            Email = "root@example.com",
            Password = storedPassword,
            IsAdmin = true
        };

        Console.WriteLine($"Default user created: Username = root, Password = {randomPassword}");
    }

    // Endpoint to request a verification code
    [HttpPost("RequestVerificationCode")]
    public async Task<IActionResult> RequestVerificationCode([FromBody] VerificationRequest request)
    {
        if (!_users.TryGetValue(request.Username, out var user) || !VerifyPassword(request.Password, user.Password))
        {
            return Unauthorized("Invalid username or password.");
        }

        string verificationCode = "123456"; // Replace with random code if desired
        VerificationCodes[request.Username] = verificationCode;

        await _mailjetService.SendVerificationEmailAsync(user.Email, verificationCode);

        return Ok("Verification code sent to your email.");
    }

    // Endpoint to verify the code and issue a token
    [HttpPost("VerifyCode")]
    public IActionResult VerifyCode([FromBody] CodeVerificationRequest request)
    {
        if (!VerificationCodes.TryGetValue(request.Username, out var code) || code != request.VerificationCode)
        {
            return Unauthorized("Invalid verification code.");
        }

        string token = GenerateToken();
        DateTime expiry = DateTime.UtcNow.AddMinutes(15);
        Tokens[request.Username] = (token, expiry);
        VerificationCodes.Remove(request.Username);

        return Ok(new { Token = token });
    }

    // Add a new user
    [HttpPost("AddUser")]
    public async Task<IActionResult> AddUser([FromBody] UserCreateRequest userCreateRequest, [FromHeader] string token)
    {
        if (!IsTokenValid(token))
        {
            return Unauthorized("Invalid or expired token.");
        }

        if (_users.ContainsKey(userCreateRequest.Username))
            return BadRequest("User already exists.");

        string randomPassword = GenerateRandomPassword();
        byte[] salt = RandomNumberGenerator.GetBytes(128 / 8);
        string hashedPassword = Convert.ToBase64String(KeyDerivation.Pbkdf2(
            password: randomPassword,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 100000,
            numBytesRequested: 256 / 8));

        string storedPassword = $"{Convert.ToBase64String(salt)}:{hashedPassword}";

        _users[userCreateRequest.Username] = new User
        {
            Username = userCreateRequest.Username,
            Email = userCreateRequest.Email,
            Password = storedPassword,
            IsAdmin = false
        };

        await _mailjetService.SendVerificationEmailAsync(userCreateRequest.Email, randomPassword);

        return Ok($"User added. Random password: {randomPassword}");
    }

    // Modify a user
    [HttpPost("ModifyUser")]
    public IActionResult ModifyUser([FromBody] UserModifyRequest userModifyRequest, [FromHeader] string token)
    {
        if (!IsTokenValid(token))
        {
            return Unauthorized("Invalid or expired token.");
        }

        if (!_users.TryGetValue(userModifyRequest.Username, out var user))
            return NotFound("User not found");

        user.IsAdmin = userModifyRequest.IsAdmin;
        return Ok("User modified");
    }

    // Delete a user
    [HttpPost("DeleteUser")]
    public IActionResult DeleteUser([FromBody] UserDeleteRequest userDeleteRequest, [FromHeader] string token)
    {
        if (!IsTokenValid(token))
        {
            return Unauthorized("Invalid or expired token.");
        }

        if (!_users.Remove(userDeleteRequest.Username))
            return NotFound("User not found");

        return Ok("User deleted");
    }

    // Test endpoint without token verification
    [HttpGet("Test")]
    public IActionResult Test()
    {
        return Content("huh");
    }

    // Admin console (example, without token verification for now)
    [HttpPost("AdminConsole")]
    public IActionResult AdminConsole()
    {
        return Content("Access denied");
    }

    // Audit expenses
    [HttpPost("AuditExpenses")]
    public IActionResult AuditExpenses([FromHeader] string token)
    {
        if (!IsTokenValid(token))
        {
            return Unauthorized("Invalid or expired token.");
        }

        try
        {
            var expenses = System.IO.File.ReadAllText("expenses.txt");
            return Ok(new Dictionary<string, object> { { "e", expenses } });
        }
        catch (FileNotFoundException)
        {
            return Ok(new Dictionary<string, object> { { "e", "none" } });
        }
    }

    // Add an expense
    [HttpPost("AddExpense")]
    public IActionResult AddExpense([FromBody] Dictionary<string, string> newExpense, [FromHeader] string token)
    {
        if (!IsTokenValid(token))
        {
            return Unauthorized("Invalid or expired token.");
        }

        try
        {
            System.IO.File.AppendAllText("expenses.txt", newExpense["value"] + "\n");
            return Content("Expense added");
        }
        catch (IOException)
        {
            return Content("Unable to add expense");
        }
    }

    // Audit timesheets
    [HttpPost("AuditTimesheets")]
    public IActionResult AuditTimesheets([FromHeader] string token)
    {
        if (!IsTokenValid(token))
        {
            return Unauthorized("Invalid or expired token.");
        }

        try
        {
            var timesheets = System.IO.File.ReadAllText("timesheets.txt");
            return Content(timesheets);
        }
        catch (FileNotFoundException)
        {
            return Content("No timesheets yet");
        }
    }

    // Submit a timesheet
    [HttpPost("SubmitTimesheet")]
    public IActionResult SubmitTimesheet([FromBody] string newTimesheet, [FromHeader] string token)
    {
        if (!IsTokenValid(token))
        {
            return Unauthorized("Invalid or expired token.");
        }

        try
        {
            System.IO.File.AppendAllText("timesheets.txt", newTimesheet + "\n");
            return Content("Timesheet added");
        }
        catch (IOException)
        {
            return Content("Unable to add timesheet");
        }
    }

    // View meeting minutes
    [HttpPost("ViewMeetingMinutes")]
    public IActionResult ViewMeetingMinutes([FromHeader] string token)
    {
        if (!IsTokenValid(token))
        {
            return Unauthorized("Invalid or expired token.");
        }

        try
        {
            var meetingMinutes = System.IO.File.ReadAllText("meeting_minutes.txt");
            return Content(meetingMinutes);
        }
        catch (FileNotFoundException)
        {
            return Content("No meeting minutes yet");
        }
    }

    // Add meeting minutes
    [HttpPost("AddMeetingMinutes")]
    public IActionResult AddMeetingMinutes([FromBody] string newMeetingMinutes, [FromHeader] string token)
    {
        if (!IsTokenValid(token))
        {
            return Unauthorized("Invalid or expired token.");
        }

        try
        {
            System.IO.File.AppendAllText("meeting_minutes.txt", newMeetingMinutes + "\n");
            return Content("Meeting minutes added");
        }
        catch (IOException)
        {
            return Content("Unable to add meeting minutes");
        }
    }

    // View roster
    [HttpPost("ViewRoster")]
    public IActionResult ViewRoster([FromHeader] string token)
    {
        if (!IsTokenValid(token))
        {
            return Unauthorized("Invalid or expired token.");
        }

        try
        {
            var roster = System.IO.File.ReadAllText("roster.txt");
            return Content(roster);
        }
        catch (FileNotFoundException)
        {
            return Content("No roster yet");
        }
    }

    // Add to roster
    [HttpPost("AddToRoster")]
    public IActionResult AddToRoster([FromBody] string newRosterEntry, [FromHeader] string token)
    {
        if (!IsTokenValid(token))
        {
            return Unauthorized("Invalid or expired token.");
        }

        try
        {
            System.IO.File.AppendAllText("roster.txt", newRosterEntry + "\n");
            return Content("Roster entry added");
        }
        catch (IOException)
        {
            return Content("Unable to add to roster");
        }
    }

    private bool IsTokenValid(string token)
    {
        // Implement your token validation logic here
        return Tokens.Any(t => t.Value.Token == token && t.Value.Expiry > DateTime.UtcNow);
    }

    private static string GenerateToken()
    {
        // Implement your token generation logic here
        return Guid.NewGuid().ToString();
    }

    private static string GenerateRandomPassword()
    {
        // Implement your random password generation logic here
        return "randomPassword"; // Replace with actual logic
    }

    private static bool VerifyPassword(string inputPassword, string storedPassword)
    {
        // Implement your password verification logic here
        return true; // Replace with actual logic
    }
}
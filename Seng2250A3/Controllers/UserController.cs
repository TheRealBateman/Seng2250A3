using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Mvc;
using Seng2250A3.Enums;
using Seng2250A3.Models;
using Seng2250A3.Requests;
using Seng2250A3.Services;
using Seng2250A3.Utils;

namespace Seng2250A3.Controllers;

[ApiController]
[Route("[controller]")]
public class UserController : Controller
{
    private readonly IUserService _userService;
    private readonly IMailjetService _mailjetService;
    private static readonly Dictionary<string, string> VerificationCodes = new();

    public UserController(IUserService userService, IMailjetService mailjetService)
    {
        _userService = userService;
        _mailjetService = mailjetService;
    }

    // Admin Console Endpoint
    [HttpPost("AdminConsole")]
    public async Task<IActionResult> AdminConsole([FromBody] AdminConsoleRequest request)
    {
        var authorizationHeader = HttpContext.Request.Headers["Authorization"];
        var token = authorizationHeader.ToString().Replace("Bearer ", "").Trim();
        Console.WriteLine();
        if (!AuthUtils.IsTokenValid(token))
        {
            return Unauthorized("Invalid or expired token.");
        }

        if (request.Command != CommandType.AddUser)
        {
            var user = _userService.GetUser(request.Username);
            if (user == null)
            {
                return NotFound($"{request.Username} not found");
            }
            return request.Command switch
            {
                CommandType.AddUser => await HandleAddUser(request),
                CommandType.ModifyUser => HandleModifyUser(request, user),
                CommandType.DeleteUser => HandleDeleteUser(request.Username),
                _ => BadRequest("Invalid command type.")
            };
        }

        return request.Command switch
        {
            CommandType.AddUser => await HandleAddUser(request),
            _ => BadRequest("Invalid command type.")
        };
    }

    private async Task<IActionResult> HandleAddUser(AdminConsoleRequest request)
    {
        string randomPassword = AuthUtils.GenerateRandomPassword();
        string hashedPasswordWithSalt = AuthUtils.HashPassword(randomPassword, out _);

        var newUser = new User
        {
            Username = request.Username,
            Email = request.Email,
            Password = hashedPasswordWithSalt,
            SecurityLevel = request.SecurityLevel,
            IsAdmin = request.IsAdmin ?? false
        };

        _userService.AddUser(newUser);
        await _mailjetService.SendUserDetailsEmailAsync("batesysgaming@gmail.com", newUser.Username, randomPassword);

        return Ok($"User {request.Username} has been created.");
    }

    private IActionResult HandleModifyUser(AdminConsoleRequest request, User user)
    {
        user.SecurityLevel = request.SecurityLevel;

        var result = _userService.ModifyUser(user.Username, user);
        if (!result)
        {
            return BadRequest($"Failed to update user {request.Username}.");
        }

        return Ok($"User {request.Username} modified. New SecurityLevel: {request.SecurityLevel}");
    }

    private IActionResult HandleDeleteUser(string username)
    {
        var result = _userService.DeleteUser(username);
        if (!result)
        {
            return NotFound($"User {username} not found.");
        }

        return Ok($"User {username} deleted.");
    }

    // User Login Endpoint
    [HttpPost("Login")]
    public async Task<IActionResult> Login([FromBody] VerificationRequest request)
    {
        var user = _userService.GetUser(request.Username);
        if (user == null || !AuthUtils.VerifyPassword(request.Password, user.Password))
        {
            return Unauthorized("Invalid username or password.");
        }

        string verificationCode = "123456"; // Replace with random code generation if desired
        VerificationCodes[request.Username] = verificationCode;

        await _mailjetService.SendVerificationEmailAsync(user.Email, verificationCode);
        return Ok("Verification code sent to your email.");
    }

    // Verify Code Endpoint
    [HttpPost("VerifyCode")]
    public IActionResult VerifyCode([FromBody] CodeVerificationRequest request)
    {
        if (!VerificationCodes.TryGetValue(request.Username, out var code) || code != request.VerificationCode)
        {
            return Unauthorized("Invalid verification code.");
        }

        var user = _userService.GetUser(request.Username);
        if (user == null)
        {
            return NotFound("User not found.");
        }

        string token = AuthUtils.GenerateToken(user.SecurityLevel);
        VerificationCodes.Remove(request.Username);
        return Ok(new { Token = token });
    }

    // Add Expense Endpoint
    [HttpPost("AddExpense")]
    public IActionResult AddExpense([FromBody] Dictionary<string, string> newExpense)
    {
        var authorizationHeader = HttpContext.Request.Headers["Authorization"];
        var token = authorizationHeader.ToString().Replace("Bearer ", "").Trim();

        if (!AuthUtils.IsTokenValid(token))
        {
            return Unauthorized("Invalid or expired token.");
        }

        var securityLevel = AuthUtils.Tokens[token].SecurityLevel;

        if (securityLevel != SecurityLevel.TopSecret)
        {
            return Unauthorized("You do not have permission to add expenses.");
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

    // Audit expenses
    [HttpPost("AuditExpenses")]
    public IActionResult AuditExpenses()
    {
        var authorizationHeader = HttpContext.Request.Headers["Authorization"];
        var token = authorizationHeader.ToString().Replace("Bearer ", "").Trim();
        if (!AuthUtils.IsTokenValid(token))
        {
            return Unauthorized("Invalid or expired token.");
        }

        var securityLevel = AuthUtils.Tokens[token].SecurityLevel;

        // Apply Biba model rules for access control
        if (!(securityLevel == SecurityLevel.TopSecret || securityLevel == SecurityLevel.Secret || securityLevel == SecurityLevel.Unclassified))
        {
            return Unauthorized("You do not have permission to audit expenses.");
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

    // Audit timesheets
    [HttpPost("AuditTimesheets")]
    public IActionResult AuditTimesheets()
    {
        var authorizationHeader = HttpContext.Request.Headers["Authorization"];
        var token = authorizationHeader.ToString().Replace("Bearer ", "").Trim();
        if (!AuthUtils.IsTokenValid(token))
        {
            return Unauthorized("Invalid or expired token.");
        }

        var securityLevel = AuthUtils.Tokens[token].SecurityLevel;

        // Apply Biba model rules for access control
        if (!(securityLevel == SecurityLevel.TopSecret || securityLevel == SecurityLevel.Secret || securityLevel == SecurityLevel.Unclassified))
        {
            return Unauthorized("You do not have permission to audit timesheets.");
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
    public IActionResult SubmitTimesheet([FromBody] string newTimesheet)
    {
        var authorizationHeader = HttpContext.Request.Headers["Authorization"];
        var token = authorizationHeader.ToString().Replace("Bearer ", "").Trim();
        if (!AuthUtils.IsTokenValid(token))
        {
            return Unauthorized("Invalid or expired token.");
        }

        var securityLevel = AuthUtils.Tokens[token].SecurityLevel;

        // Apply Biba model rules for access control
        if (securityLevel == SecurityLevel.Secret || securityLevel == SecurityLevel.Unclassified)
        {
            return Unauthorized("You do not have permission to submit timesheets.");
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
    public IActionResult ViewMeetingMinutes()
    {
        var authorizationHeader = HttpContext.Request.Headers["Authorization"];
        var token = authorizationHeader.ToString().Replace("Bearer ", "").Trim();
        if (!AuthUtils.IsTokenValid(token))
        {
            return Unauthorized("Invalid or expired token.");
        }

        var securityLevel = AuthUtils.Tokens[token].SecurityLevel;

        // Apply Biba model rules for access control
        if (securityLevel == SecurityLevel.TopSecret)
        {
            return Unauthorized("You do not have permission to view meeting minutes.");
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
    public IActionResult AddMeetingMinutes([FromBody] string newMeetingMinutes)
    {
        var authorizationHeader = HttpContext.Request.Headers["Authorization"];
        var token = authorizationHeader.ToString().Replace("Bearer ", "").Trim();
        if (!AuthUtils.IsTokenValid(token))
        {
            return Unauthorized("Invalid or expired token.");
        }
        var securityLevel = AuthUtils.Tokens[token].SecurityLevel;

        // Apply Biba model rules for access control
        if (securityLevel != SecurityLevel.TopSecret && securityLevel != SecurityLevel.Secret)
        {
            return Unauthorized("You do not have permission to add meeting minutes.");
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
    public IActionResult ViewRoster()
    {
        var authorizationHeader = HttpContext.Request.Headers["Authorization"];
        var token = authorizationHeader.ToString().Replace("Bearer ", "").Trim();
        if (!AuthUtils.IsTokenValid(token))
        {
            return Unauthorized("Invalid or expired token.");
        }

        var securityLevel = AuthUtils.Tokens[token].SecurityLevel;

        // Apply Biba model rules for access control
        if (securityLevel == SecurityLevel.Secret || securityLevel == SecurityLevel.TopSecret)
        {
            return Unauthorized("You do not have permission to view the roster.");
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
    public IActionResult AddToRoster([FromBody] string newRosterEntry)
    {
        var authorizationHeader = HttpContext.Request.Headers["Authorization"];
        var token = authorizationHeader.ToString().Replace("Bearer ", "").Trim();
        if (!AuthUtils.IsTokenValid(token))
        {
            return Unauthorized("Invalid or expired token.");
        }

        var securityLevel = AuthUtils.Tokens[token].SecurityLevel;
        
        if (!(securityLevel == SecurityLevel.TopSecret || securityLevel == SecurityLevel.Secret || securityLevel == SecurityLevel.Unclassified))
        {
            return Unauthorized("You do not have permission to add to the roster.");
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
}

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Seng2250A3.Enums;

namespace Client;

public class Client
{
    private static readonly HttpClient client = new();

    public static async Task<string> PostAsync(string url, object data, string token = null)
    {
        using (var httpClient = new HttpClient())
        {
            var jsonContent = JsonSerializer.Serialize(data);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // Add the authorization header
            if (!string.IsNullOrEmpty(token))
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var response = await httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return errorContent;
            }

            return await response.Content.ReadAsStringAsync();
        }
    }
    public static async Task Main(string[] args)
    {
        //Admin login
        Console.Write("Enter your admin username: ");
        string adminUsername = Console.ReadLine();

        Console.Write("Enter your admin password: ");
        string adminPassword = Console.ReadLine();

        // Request a verification code
        var adminVerificationRequest = new { Username = adminUsername, Password = adminPassword };
        Console.WriteLine(await PostAsync("http://localhost:7272/User/Login", adminVerificationRequest));

        // Input verification code
        Console.Write("Enter the verification code sent to your email: ");
        string adminVerificationCode = Console.ReadLine();
        var adminVerificationResponse = await PostAsync("http://localhost:7272/User/VerifyCode",
            new { Username = adminUsername, VerificationCode = adminVerificationCode });
        string adminToken = ExtractToken(adminVerificationResponse);
        
        string userEmail = "batesysgaming@gmail.com"; // Set a fixed email for all users
        foreach (SecurityLevel securityLevel in Enum.GetValues(typeof(SecurityLevel)))
        {
            var addUserResponse = await PostAsync("http://localhost:7272/User/AdminConsole", new
            {
                Command = CommandType.AddUser,
                Username = $"user_{securityLevel}",
                Email = userEmail, 
                IsAdmin = false,
                SecurityLevel = securityLevel
            }, adminToken);

            Console.WriteLine($"Add User Response ({securityLevel}): " + addUserResponse);
        }
        
        // Example of modifying a user
        var modifyUserResponse = await PostAsync("http://localhost:7272/User/AdminConsole", new
        {
            Command = CommandType.ModifyUser,
            Username = "user_Secret",
            SecurityLevel = SecurityLevel.TopSecret
        }, adminToken);
        Console.WriteLine("Modify User Response: " + modifyUserResponse);

        modifyUserResponse = await PostAsync("http://localhost:7272/User/AdminConsole", new
        {
            Command = CommandType.ModifyUser,
            Username = "user_Secret",
            SecurityLevel = SecurityLevel.Secret
        }, adminToken);
        Console.WriteLine("Modify User Response: " + modifyUserResponse);

        var addUserToDeleteResponse = await PostAsync("http://localhost:7272/User/AdminConsole", new
        {
            Command = CommandType.AddUser,
            Username = $"userdelete",
            Email = userEmail,
            IsAdmin = false,
            SecurityLevel = SecurityLevel.TopSecret
        }, adminToken);

        Console.WriteLine($"Add User Response: " + addUserToDeleteResponse);
       
        // Example of deleting a user
        var deleteUserResponse = await PostAsync("http://localhost:7272/User/AdminConsole", new
        {
            Command = CommandType.DeleteUser,
            Username = "userdelete" // Example username, modify as needed
        }, adminToken);
        Console.WriteLine("Delete User Response: " + deleteUserResponse);


        // Test adding an expense
        Console.WriteLine(await PostAsync("http://localhost:7272/User/AddExpense", new { value = "test1" },
            adminToken));

        // Test auditing expenses
        Console.WriteLine(
            "Expenses: " + await PostAsync("http://localhost:7272/User/AuditExpenses", new { }, adminToken));

        // Test adding meeting minutes
        const string newMeetingMinutes = "newMeetingMinutes";
        Console.WriteLine(await PostAsync("http://localhost:7272/User/AddMeetingMinutes", "Meeting notes for today",
            adminToken));


        // Test viewing meeting minutes
        Console.WriteLine("Meeting Minutes: " +
                          await PostAsync("http://localhost:7272/User/ViewMeetingMinutes", new { }, adminToken));

        // Test submitting a timesheet
        Console.WriteLine(await PostAsync("http://localhost:7272/User/SubmitTimesheet",
            "Timesheet data", adminToken));

        // Test auditing timesheets
        Console.WriteLine("Timesheets: " +
                          await PostAsync("http://localhost:7272/User/AuditTimesheets", new { }, adminToken));

        // Test adding to roster
        Console.WriteLine(await PostAsync("http://localhost:7272/User/AddToRoster",
            "Shift data", adminToken));

        // Test viewing the roster
        Console.WriteLine("Roster: " + await PostAsync("http://localhost:7272/User/ViewRoster", new { }, adminToken));
        //Verify and login each user
        foreach (SecurityLevel securityLevel in Enum.GetValues(typeof(SecurityLevel)))
        {
            string userUsername = $"user_{securityLevel}";

            var loginRequest = new { Username = userUsername, Password = "123456" };
            Console.WriteLine("\n" + $"Logging in as {userUsername}");
            var loginResponse = await PostAsync("http://localhost:7272/User/Login\n", loginRequest);

            if (string.IsNullOrEmpty(loginResponse))
            {
                Console.WriteLine($"Login failed for {userUsername}");
                return;
            }

            string verificationCode = "123456";

            string userToken = await VerifyUser(userUsername, verificationCode);

            if (string.IsNullOrEmpty(userToken))
            {
                Console.WriteLine($"Failed to login as {userUsername}");
                continue;
            }

            Console.WriteLine($"Running tests for {userUsername} of type: {securityLevel}:");

            // Test adding an expense
            Console.WriteLine(await PostAsync("http://localhost:7272/User/AddExpense", new { value = "test1" },
                userToken));

            // Test auditing expenses
            Console.WriteLine(
                "Expenses: " + await PostAsync("http://localhost:7272/User/AuditExpenses", new { }, userToken));

            // Test adding meeting minutes
            Console.WriteLine(await PostAsync("http://localhost:7272/User/AddMeetingMinutes", "Meeting notes for today",
                userToken));


            // Test viewing meeting minutes
            Console.WriteLine("Meeting Minutes: " +
                              await PostAsync("http://localhost:7272/User/ViewMeetingMinutes", new { }, userToken));

            // Test submitting a timesheet
            Console.WriteLine(await PostAsync("http://localhost:7272/User/SubmitTimesheet",
                "Timesheet data", userToken));

            // Test auditing timesheets
            Console.WriteLine("Timesheets: " +
                              await PostAsync("http://localhost:7272/User/AuditTimesheets", new { }, userToken));

            // Test adding to roster
            Console.WriteLine(await PostAsync("http://localhost:7272/User/AddToRoster",
                "Shift data", userToken));

            // Test viewing the roster
            Console.WriteLine("Roster: " + await PostAsync("http://localhost:7272/User/ViewRoster", new { }, userToken));
        }
        
        //Testing invalid tokens and no token
        string invalidToken = "invalidToken123";
        Console.WriteLine("\nTesting with invalid token:");
        var response = await PostAsync("http://localhost:7272/User/AddExpense", new { value = "test" }, invalidToken);
        Console.WriteLine("Add Expense with invalid token: " + response);

        response = await PostAsync("http://localhost:7272/User/ViewRoster", new { });
        Console.WriteLine("View Roster with invalid token: " + response);
    }

    private static async Task<string> VerifyUser(string username, string verificationCode)
    {
        var verificationRequest = new { Username = username, VerificationCode = verificationCode };
        var verificationResponse = await PostAsync("http://localhost:7272/User/VerifyCode", verificationRequest);
        var userToken = ExtractToken(verificationResponse);
        return userToken;
    }

    private static string ExtractToken(string response)
    {
        using (JsonDocument doc = JsonDocument.Parse(response))
        {
            if (doc.RootElement.TryGetProperty("token", out JsonElement tokenElement))
            {
                return tokenElement.GetString();
            }

            return null;
        }
    }
}
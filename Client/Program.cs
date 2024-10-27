using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Client
{
    public class Client
    {
        private static readonly HttpClient client = new();

        public static async Task<string> GetAsync(string url, string token = null)
        {
            try
            {
                if (!string.IsNullOrEmpty(token))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException e)
            {
                return $"Request error: {e.Message}";
            }
        }

        public static async Task<string> PostAsync(string url, object data, string token = null)
        {
            try
            {
                var jsonData = JsonSerializer.Serialize(data);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                if (!string.IsNullOrEmpty(token))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }

                HttpResponseMessage response = await client.PostAsync(url, content);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException e)
            {
                return $"Request error: {e.Message}";
            }
        }

        public static async Task Main(string[] args)
        {
            // Prompt for username and password
            Console.Write("Enter your username: ");
            string username = Console.ReadLine();

            Console.Write("Enter your password: ");
            string password = Console.ReadLine(); // Note: Be cautious with storing passwords in plain text

            // Request a verification code
            var verificationRequest = new { Username = username, Password = password };
            Console.WriteLine(await PostAsync("http://localhost:7272/RequestVerificationCode", verificationRequest));

            // Input verification code
            Console.Write("Enter the verification code sent to your email: ");
            string verificationCode = Console.ReadLine();
            var verificationCodeRequest = new { Username = username, VerificationCode = verificationCode };
            var verificationResponse = await PostAsync("http://localhost:7272/VerifyCode", verificationCodeRequest);
            Console.WriteLine(verificationResponse);

            // Extract the token from the verification response
            string token = ""; // Initialize the token

            using (JsonDocument doc = JsonDocument.Parse(verificationResponse))
            {
                if (doc.RootElement.TryGetProperty("Token", out JsonElement tokenElement))
                {
                    token = tokenElement.GetString();
                }
                else
                {
                    Console.WriteLine("Failed to retrieve token.");
                    return;
                }
            }

            // Test adding an expense
            Console.WriteLine(await PostAsync("http://localhost:7272/AddExpense", new { value = "test1" }, token));

            // Test adding a user
            var newUser = new { Username = "testuser", Email = "testuser@example.com" };
            Console.WriteLine(await PostAsync("http://localhost:7272/AddUser", newUser, token));

            // Test modifying a user
            var modifyUser = new { Username = "testuser", IsAdmin = true };
            Console.WriteLine(await PostAsync("http://localhost:7272/ModifyUser", modifyUser, token));

            // Test deleting a user
            var deleteUser = new { Username = "testuser" };
            Console.WriteLine(await PostAsync("http://localhost:7272/DeleteUser", deleteUser, token));

            // Test auditing expenses
            Console.WriteLine(await PostAsync("http://localhost:7272/AuditExpenses", new { }, token));

            // Test adding meeting minutes
            Console.WriteLine(await PostAsync("http://localhost:7272/AddMeetingMinutes", new { notes = "Meeting notes for today" }, token));

            // Test viewing meeting minutes
            Console.WriteLine(await PostAsync("http://localhost:7272/ViewMeetingMinutes", new { }, token));

            // Test submitting a timesheet
            Console.WriteLine(await PostAsync("http://localhost:7272/SubmitTimesheet", new { data = "Timesheet data" }, token));

            // Test auditing timesheets
            Console.WriteLine(await PostAsync("http://localhost:7272/AuditTimesheets", new { }, token));

            // Test adding to roster
            Console.WriteLine(await PostAsync("http://localhost:7272/AddRosterShift", new { shiftData = "Shift data" }, token));

            // Test viewing the roster
            Console.WriteLine(await PostAsync("http://localhost:7272/ViewRoster", new { }, token));
        }
    }
}

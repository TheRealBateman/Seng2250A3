namespace Seng2250A3;

public class User
{
    public string Username { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public bool IsAdmin { get; set; }
    
    public override string ToString()
    {
        return $"Username: {Username}, Email: {Email}, IsAdmin: {IsAdmin}";
    }
}
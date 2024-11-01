using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Seng2250A3;
using Seng2250A3.Enums;
using Seng2250A3.Models;
using Seng2250A3.Services;
using Seng2250A3.Utils;


public interface IUserService
{
    User  GetUser(string username);
    
    public bool ModifyUser(string username, User updatedUser);
    
    void InitializeDefaultUser();
    
    void AddUser(User user);
    
    bool DeleteUser(string username);
}

public class UserService: IUserService
{
    private readonly Dictionary<string, User> _users = new();
    private bool _isDefaultUserInitialized = false;
    public UserService()
    {
        InitializeDefaultUser();
    }
    
    public void InitializeDefaultUser()
    {
        if (_isDefaultUserInitialized) return;
        string randomPassword = AuthUtils.GenerateRandomPassword();
        string hashedPasswordWithSalt = AuthUtils.HashPassword(randomPassword, out _);
        

        _users["root"] = new User
        {
            Username = "root",
            Email = "batesysgaming@gmail.com",
            Password = hashedPasswordWithSalt,
            IsAdmin = true,
            SecurityLevel = SecurityLevel.TopSecret
        };
      
        _isDefaultUserInitialized = true;
        Console.WriteLine($"Default user created: Username = root, Password = {randomPassword}");
    }

   

    public User GetUser(string username)
    {
        _users.TryGetValue(username, out User user);
        return user;
    }
    public void AddUser(User user)
    {
        _users[user.Username] = user;
    }

    public bool ModifyUser(string username, User updatedUser)
    {
        if (_users.ContainsKey(username))
        {
            _users[username] = updatedUser;
            return true;
        }
        return false; 
    }

    public bool DeleteUser(string username) => _users.Remove(username);
}
using System.Security.Cryptography;
using API.Data;
using API.Entities;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

public class AccountController(AppDbContext context) : BaseController
{
    [HttpPost("register")]
    public async Task<ActionResult<AppUser>> Register(string email, string displayName, string password)
    {
        using var hmac = new HMACSHA512();
        var appUser = new AppUser
        {
            Email = email,
            DisplayName = displayName,
            PasswordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password)),
            PasswordSalt =hmac.Key
        };
        context.Users.Add(appUser);
        await context.SaveChangesAsync();
        return appUser;
    }
}

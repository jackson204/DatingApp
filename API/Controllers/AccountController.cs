using System.Security.Cryptography;
using API.Data;
using API.DTOs;
using API.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

public class AccountController(AppDbContext context) : BaseController
{
    [HttpPost("register")]
    public async Task<ActionResult<AppUser>> Register(RegisterDto registerDto)
    {
        if (await EmailExists(registerDto.Email))
        {
            return BadRequest("Email already exists");
        }
        using var hmac = new HMACSHA512();
        var appUser = new AppUser
        {
            Email = registerDto.Email,
            DisplayName = registerDto.DisplayName,
            PasswordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(registerDto.Password)),
            PasswordSalt = hmac.Key
        };
        context.Users.Add(appUser);
        await context.SaveChangesAsync();
        return appUser;
    }

    private async Task<bool> EmailExists(string email)
    {
        return await context.Users.AnyAsync(u => u.Email.ToLower() == email.ToLower());
    }
}

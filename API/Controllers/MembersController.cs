using API.Data;
using API.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;


public class MembersController(AppDbContext dbContext) : BaseController
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AppUser>>> GetMembers()
    {
        return await dbContext.Users.ToListAsync();
    }
    
    [Authorize]
    [HttpGet]
    [Route("{id}")]
    public async Task<ActionResult<AppUser>> GetMember(string id)
    {
        var user = await dbContext.Users.FindAsync(id);
        if (user ==null)
        {
            return NotFound();
        }
        return user;
    }
}


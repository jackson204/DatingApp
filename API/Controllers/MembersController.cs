using API.Data;
using API.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[ApiController]
[Route("[controller]")]
public class MembersController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AppUser>>> GetMembers()
    {
        return await dbContext.Users.ToListAsync();
    }
    
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


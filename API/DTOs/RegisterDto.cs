namespace API.DTOs;

public class RegisterDto
{
    public required string DisplayName { get; set; }

    public required string Email { get; set; }

    public required  string Password { get; set; }
}

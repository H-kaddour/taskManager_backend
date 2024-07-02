using System.ComponentModel.DataAnnotations;

namespace TaskManager.Api.Dtos;

public record class LoginUserDto(
    [Required][EmailAddress()] string email,
    [Required][StringLength(100, MinimumLength = 8)] string password
);
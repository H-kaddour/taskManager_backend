using System.ComponentModel.DataAnnotations;

namespace TaskManager.Api.Dtos;

public record class RegisterUserDto(
    [Required][StringLength(15, MinimumLength = 5)] string username,
    [Required][EmailAddress()] string email,
    [Required][StringLength(100, MinimumLength = 8)] string password
);
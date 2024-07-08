using System.ComponentModel.DataAnnotations;
using TaskManager.Api.Data;
using TaskManager.Api.Entities;

namespace TaskManager.Api.Dtos;

public record class newTaskDto(
    [Required][StringLength(500, MinimumLength = 3)] string title,
    [Required][StringLength(1000, MinimumLength = 3)] string description,
    [Required] string status,
    [Required] string deadline,
    [Required] string UserId
);
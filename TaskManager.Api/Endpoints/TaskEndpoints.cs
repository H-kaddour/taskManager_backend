using TaskManager.Api.Data;
using TaskManager.Api.Dtos;
using TaskManager.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using System.Text;

using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace TaskManager.Api.Endpoints;

public static class TaskEndpoints
{
    public static string getEmailFromJwt(HttpContext httpContext) {
        var token = httpContext.Request.Cookies["jwt"];
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        var claims = jwtToken.Claims;
        var identity = new ClaimsIdentity(claims, "unique_name");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        return claimsPrincipal.Claims.FirstOrDefault(c => c.Type == "unique_name")?.Value;
    }

    public static RouteGroupBuilder MapTasksEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("api/task")
                       .WithParameterValidation();

        group.MapGet("/", [Microsoft.AspNetCore.Authorization.Authorize] async (DbStoreContext dbContext, HttpContext httpContext) => {
            var email = TaskEndpoints.getEmailFromJwt(httpContext);
            User? user = await dbContext.Users.FirstOrDefaultAsync(u => u.email == email);
            if (user is null)
                return Results.Json(new { message = "User not exists" }, statusCode: 404);
            return Results.Ok(await dbContext.Tasks
                    .Where(t => t.UserId == user.id)
                    .ToListAsync());        
        });

        group.MapGet("/{id}", [Microsoft.AspNetCore.Authorization.Authorize] async (string id, DbStoreContext dbContext) => {
            Task_m? task = await dbContext.Tasks.FirstOrDefaultAsync(u => u.id == id);
            return task is null ? Results.Json(new { message = "This id not found in tasks" }, statusCode: 404) : Results.Ok(task);
        });

        group.MapPost("/", [Microsoft.AspNetCore.Authorization.Authorize] async (newTaskDto newTask, HttpContext httpContext, DbStoreContext dbContext) => {
            var uniqueName = TaskEndpoints.getEmailFromJwt(httpContext);
            
            if (newTask.status != "open" && newTask.status != "progress" && newTask.status != "completed")
                return Results.Json(new { message = "status value should be either: open || progress || completed"}, statusCode: 400);
            Task_m task = new() {
                id = (Guid.NewGuid()).ToString(),
                title = newTask.title,
                description = newTask.description,
                status = newTask.status,
                deadline = newTask.deadline,
                UserId = newTask.UserId,
                user = await dbContext.Users.FirstOrDefaultAsync(u => u.email == uniqueName)
            };
            dbContext.Tasks.Add(task);
            await dbContext.SaveChangesAsync();
            task.user.password = null;
            return Results.Ok(task);
        });

        group.MapPut("/{id}", [Microsoft.AspNetCore.Authorization.Authorize] async (string id, updateTaskDto newTask, DbStoreContext dbContext) => {
            var existingTask = await dbContext.Tasks.FindAsync(id);

            if (existingTask is null)
                return Results.NotFound();

            if (newTask.status != "open" && newTask.status != "progress" && newTask.status != "completed")
                return Results.Json(new { message = "status value should be either: open || progress || completed"}, statusCode: 400);
            Task_m task = new() {
                id = id,
                title = newTask.title,
                description = newTask.description,
                status = newTask.status,
                deadline = newTask.deadline,
                UserId = existingTask.UserId,
                user = existingTask.user
            };
            existingTask.title = newTask.title;
            existingTask.description = newTask.description;
            existingTask.status = newTask.status;
            existingTask.deadline = newTask.deadline;

            dbContext.Tasks.Update(existingTask);
            await dbContext.SaveChangesAsync();

            return Results.NoContent();
        });

        group.MapDelete("/{id}", [Microsoft.AspNetCore.Authorization.Authorize] async (string id, DbStoreContext dbContext) => {
            await dbContext.Tasks
                     .Where(task => task.id == id)
                     .ExecuteDeleteAsync();

            return Results.NoContent();
        });

        return group;
    }
}

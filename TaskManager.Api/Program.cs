using TaskManager.Api.Data;
using TaskManager.Api.Entities;
using TaskManager.Api.Dtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;

using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

var connString = builder.Configuration.GetConnectionString("DbConnection");

var jwtSettings = builder.Configuration.GetSection("JwtSettings");

builder.Services.AddAuthentication(opt => {
    opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options => {
    options.TokenValidationParameters = new TokenValidationParameters {
        ValidateIssuer = true,
        //ValidateAudience = true,
        ValidateLifetime = true,
        //ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Link"],
        ValidAudience = jwtSettings["Link"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]))
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            context.Token = context.Request.Cookies["jwt"];
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

builder.Services.AddEntityFrameworkNpgsql().AddDbContext<DbStoreContext>(options => {
    options.UseNpgsql(connString);
});

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

void logicOfJwt(string email, HttpResponse res) {
    var tokenHandler = new JwtSecurityTokenHandler();
    var key = Encoding.ASCII.GetBytes(jwtSettings["Key"]);
    var tokenDescriptor = new SecurityTokenDescriptor {
        Subject = new ClaimsIdentity(new Claim[] {
            new Claim(ClaimTypes.Name, email) //i want to add here the id
        }),
        Expires = DateTime.UtcNow.AddMinutes(30),
        Issuer = jwtSettings["Link"],
        Audience = jwtSettings["Link"],
        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
    };
    var token = tokenHandler.CreateToken(tokenDescriptor);
    var tokenString = tokenHandler.WriteToken(token);
    // Set the token in a cookie
    var cookieOptions = new CookieOptions{
        HttpOnly = true,
        Secure = false,
        SameSite = SameSiteMode.Strict,
        Expires = DateTime.UtcNow.AddMinutes(30)
    };
    res.Cookies.Append("jwt", tokenString, cookieOptions);
}

app.MapPost("api/auth/register", async (RegisterUserDto data, HttpResponse res, HttpContext httpContext, DbStoreContext dbContext) => {
    //if the user exist just log him in
    //add data in db
    //make the jwt
    //here you should redirect
    //encrypt password
    if (httpContext.User.Identity?.IsAuthenticated == true)
        return Results.Json(new { message = "Already register and loged in" }, statusCode: 403);
    User? check = await dbContext.Users.FirstOrDefaultAsync(u => u.email == data.email);
    if (check != null)
        return Results.Json(new { message = "This email already register" }, statusCode: 403);
    Guid newUuid = Guid.NewGuid();
    User user = new() {
        id = newUuid.ToString(),
        fullname = data.fullname,
        email = data.email,
        password = data.password
    };
    dbContext.Users.Add(user);
    await dbContext.SaveChangesAsync();
    //here create the jwt and send it
    logicOfJwt(data.email, res);
    return Results.Ok(new { Message = "Register successfully" });
}).WithParameterValidation();

app.MapPost("api/auth/login", async (LoginUserDto data, HttpResponse res, DbStoreContext dbContext, HttpContext httpContext) => {
    //here check if the user is register and credential is right
    if (httpContext.User.Identity?.IsAuthenticated == true)
        return Results.Json(new { message = "Already loged in" }, statusCode: 403);
    User? check = await dbContext.Users.FirstOrDefaultAsync(u => u.email == data.email);
    if (check == null)
        return Results.Json(new { message = "This email doesn't exists" }, statusCode: 404);
    //here i should encrypt pw but i don't have time
    if (check.password != data.password)
        return Results.Json(new { message = "Credentials are not correct!" }, statusCode: 403);
    logicOfJwt(data.email, res);
    return Results.Ok(new { Message = "Logged in successfully" });
}).WithParameterValidation(); //check input *************


app.MapGet("/protected", [Microsoft.AspNetCore.Authorization.Authorize] () => {
    return Results.Ok("This is a protected endpoint. Only authenticated users can access this.");
});


app.Run();
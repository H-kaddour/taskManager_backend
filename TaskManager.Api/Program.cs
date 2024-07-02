using TaskManager.Api.Data;
using TaskManager.Api.Entities;
using TaskManager.Api.Dtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;

using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

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

// POST /register
app.MapPost("api/auth/register", async (RegisterUserDto data, HttpContext httpContext, DbStoreContext dbContext) => {
    //if the user exist just log him in
    //add data in db
    //make the jwt
    //here you should redirect
    //encrypt password
    if (httpContext.User.Identity?.IsAuthenticated == true)
        return ;
    //User? check = await dbContext.Users.FindAsync(data.email);
    //if (check != null)
    //    return ;
    Guid newUuid = Guid.NewGuid();
    User user = new() {
        id = newUuid.ToString(),
        username = data.username,
        email = data.email,
        password = data.password
    };
    dbContext.Users.Add(user);
    await dbContext.SaveChangesAsync();
});

app.MapPost("api/auth/login", async (LoginUserDto data, HttpResponse res, DbStoreContext dbContext) => {
    //here check if the user is register and credential is right
    var tokenHandler = new JwtSecurityTokenHandler();
    var key = Encoding.ASCII.GetBytes(jwtSettings["Key"]);
    var tokenDescriptor = new SecurityTokenDescriptor {
        Subject = new ClaimsIdentity(new Claim[] {
            new Claim(ClaimTypes.Name, data.email)
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
    return Results.Ok(new { Message = "Logged in successfully" });
});


app.MapGet("/protected", [Microsoft.AspNetCore.Authorization.Authorize] () => {
    return Results.Ok("This is a protected endpoint. Only authenticated users can access this.");
});

app.Run();
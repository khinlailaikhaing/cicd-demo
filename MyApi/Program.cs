using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var jwt=builder.Configuration.GetSection("Jwt");
var key=Encoding.UTF8.GetBytes(jwt["Key"]!);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>{
    options.TokenValidationParameters=new TokenValidationParameters
    {
        ValidateIssuer=true,
        ValidIssuer=jwt["Issuer"],

        ValidateAudience=true,
        ValidAudience=jwt["Audience"],

        ValidateIssuerSigningKey=true,
        IssuerSigningKey=new SymmetricSecurityKey(key),
        ValidateLifetime=true,
        ClockSkew=TimeSpan.Zero

    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.RequireAuthorization();


app.MapPost("/login", (LoginRequest req,IConfiguration config)=>
{
     if (req.Username != "khin" || req.Password != "password123")
        return Results.Unauthorized();

    var jwtSection = config.GetSection("Jwt");
    var keyBytes = Encoding.UTF8.GetBytes(jwtSection["Key"]!);

    var claims = new List<Claim>
    {
        new(ClaimTypes.Name, req.Username),
        new(ClaimTypes.Role, "Admin")   // used in Layer 2
    };

    var handler=new JsonWebTokenHandler();
    var token=handler.CreateToken(new SecurityTokenDescriptor
    {
        Issuer = jwtSection["Issuer"],
        Audience = jwtSection["Audience"],
        Subject = new ClaimsIdentity(claims),
        Expires = DateTime.UtcNow.AddMinutes(60),
        SigningCredentials = new SigningCredentials(
            new SymmetricSecurityKey(keyBytes),
            SecurityAlgorithms.HmacSha256)
    });

    return Results.Ok(new { access_token = token });
});

app.Run();

record LoginRequest(string Username, string Password);

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

public partial class Program { }
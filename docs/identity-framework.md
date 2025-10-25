## ASP.NET Core Identity + EF Core + JWT (API Setup)

### 1. Install Packages

```bash
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Tools
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package Microsoft.IdentityModel.Tokens
```

---

### 2. Create Your Models

#### `ApplicationUser.cs`

```csharp
using Microsoft.AspNetCore.Identity;

public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

---

### 3. Create the EF Core Context

#### `ApplicationDbContext.cs`

```csharp
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    // Add other entities if needed
}
```

---

### 4. Configure Identity and JWT in `Program.cs`

```csharp
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// EF Core
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

// Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// JWT configuration
var jwtSettings = configuration.GetSection("JwtSettings");
var key = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

builder.Services.AddAuthorization();
builder.Services.AddControllers();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
```

#### Example `appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=MyAppDb;Trusted_Connection=True;"
  },
  "JwtSettings": {
    "Issuer": "https://yourdomain.com",
    "Audience": "https://yourdomain.com",
    "SecretKey": "your-super-secret-key-that-should-be-long"
  }
}
```

---

### 5. Create Auth Controller

#### `AuthController.cs`

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;

    public AuthController(UserManager<ApplicationUser> userManager, IConfiguration configuration)
    {
        _userManager = userManager;
        _configuration = configuration;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        var existingUser = await _userManager.FindByEmailAsync(dto.Email);
        if (existingUser != null)
            return BadRequest("Email already in use.");

        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            DisplayName = dto.DisplayName
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return Ok("Registration successful");
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null || !await _userManager.CheckPasswordAsync(user, dto.Password))
            return Unauthorized("Invalid credentials.");

        var token = GenerateJwtToken(user);
        return Ok(new { token });
    }

    private string GenerateJwtToken(ApplicationUser user)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!));

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email!),
            new Claim("displayName", user.DisplayName),
        };

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(12),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public record RegisterDto(string Email, string Password, string DisplayName);
public record LoginDto(string Email, string Password);
```

---

### 6. Protect API Endpoints

```csharp
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ProjectsController : ControllerBase
{
    [HttpGet]
    public IActionResult GetProjects()
    {
        return Ok(new[] { "Project A", "Project B" });
    }
}
```

Requests to this endpoint must include the header:

```
Authorization: Bearer <jwt-token>
```

---

### 7. From React Frontend

When logging in:
- POST `/api/auth/login`
- Store the JWT securely (e.g., in memory or HttpOnly cookie)
- Include it in `Authorization` headers for subsequent requests.

Example (React + Axios):

```ts
const response = await axios.post('/api/auth/login', { email, password });
localStorage.setItem('token', response.data.token);

axios.defaults.headers.common['Authorization'] = `Bearer ${response.data.token}`;
```

---

### 8. (Optional) Refresh Tokens

You can extend Identity to store refresh tokens per user:

1. Add a `RefreshToken` table (userId, token, expiry).
2. Return both access + refresh tokens from `/login`.
3. Add a `/refresh` endpoint that issues a new JWT if refresh token is valid.

This improves UX for long sessions while keeping JWT short-lived.

---

### 9. EF Migration

Generate schema:

```bash
dotnet ef migrations add InitIdentity
dotnet ef database update
```

This creates the full Identity schema automatically.

---

### ✅ Summary of Key Points

| Aspect | Description |
|--------|-------------|
| Authentication | JWT Bearer (no cookies) |
| Authorization | `[Authorize]` attributes |
| User Management | ASP.NET Core Identity |
| Token Issuer | Your backend via `JwtSecurityTokenHandler` |
| Storage | EF Core database (SQL Server or similar) |
| Frontend | React app stores and uses JWTs in API calls |

---

### 🧩 Recommended Enhancements

- **Add email confirmation** using `_userManager.GenerateEmailConfirmationTokenAsync()`
- **Add refresh tokens** for longer sessions
- **Add role-based claims** if you have admin/user distinctions
- **Centralize JWT generation** in a dedicated `ITokenService`

---

Would you like me to extend this into a **full production-ready template**, including refresh tokens, role claims, and a token service class? That would make it plug-and-play for your architecture.
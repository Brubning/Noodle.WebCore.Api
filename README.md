# Noodle.WebCore.Api
.NET Core Web Api with JwtBearer authentication.

It includes AspNetCore Identity to provide database tables, SigninManager and UserManager to make password hashing, user creation, sign-in etc. easy.

## Create project
Start by creating a project with:
`dotnet new webapi`

## Add Packages
Add the following packages:
    *Microsoft.AspNetCore.Authentication.JwtBearer
    *Microsoft.AspNetCore.Identity.EntityFrameworkCore
    *Microsoft.EntityFrameworkCore
    *Microsoft.EntityFrameworkCore.Design
    *Microsoft.EntityFrameworkCore.SqlServer
    *Microsoft.Extensions.DependencyInjection
    *Microsoft.Extensions.Identity.Core
    *System.IdentityModel.Tokens.Jwt

## Set up Entity Framework
### Add Connection String
Connection string is added to `appsettings.json`
```
    "ConnectionStrings": {
        "PortalUserConnection": "Server=SQLDEV;Database=PortalUsers;User Id=DbMaker;Password=Passw0rd_"
    }
```

### Add DbContext and Models
Models are : Data\PortalUser and Data\PortalRole. The DbContext could just be created using Microsoft.AspNetCore.Identity.IdentityUser, I've added models so that I can change the names to match my namespace and so that I can extend them.
DbContext is : Data\PortalUserDbContext. This inherits from `IdentityDbContext` so that Entity Framework will build out all related tables.

### Scaffold migrations
The initial migration is included. Recreate by clearing the Migrations folder and running `dotnet ef migrations add InitialCreate`.

### Add the EntityFramework model in Startup.cs
EntityFramework is added to `ConfigureServices`:
```
    services.AddDbContext<PortalUserDbContext>(options =>
        options.UseSqlServer(
            Configuration.GetConnectionString("PortalUserConnection")));

    services.AddIdentity<PortalUser, PortalRole>(options => options.SignIn.RequireConfirmedAccount = true)
        .AddEntityFrameworkStores<PortalUserDbContext>();
```

### Update the database
Apply the migrations using `dotnet ef database update`.

## Set up Authentication
### Add Authentication provider to Startup.cs
Add the JwtBearer authentication provider and configure the TokenValidationOptions. 
```
    services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = "JwtBearer";
        options.DefaultChallengeScheme = "JwtBearer";
    })
        .AddJwtBearer("JwtBearer", options => 
        {  
            options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
            {
                IssuerSigningKey = GetSigningKey(),
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidIssuer = GetTokenIssuer(),
                ValidAudience = GetTokenAudience(),
                ValidateLifetime = true
            };
        });
```
The signing key, token audience and token issuer should all be set up to use configuration and/or user secrets. Replace the following methods:

```
    /// <summary>
    /// Get signing key from configuration
    /// </summary>
    /// <returns></returns>
    private SecurityKey GetSigningKey()
    {
        return new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(
                "ShVmYq3s6v9y$B&E)H@McQfTjWnZr4u7"));
                //Configuration["Jwt:Symmetric:Key"]));
    }

    /// <summary>
    /// Get a valid TokenAudience
    /// </summary>
    /// <returns></returns>
    private string GetTokenAudience()
    {
        //TODO Get from configuration
        //TODO Move to a configuration factory and share reference in the AccountController
        return "http://localhost";
    }

    /// <summary>
    /// Get a valid TokenIssuer
    /// </summary>
    /// <returns></returns>
    private string GetTokenIssuer()
    {
        //TODO Get from configuration
        //TODO Move to a configuration factory and share reference in the AccountController
        return "http://localhost";
    }
```

### Add Authentication to the request pipeline in Startup.cs
In `Configure` add authentication before authorization. Remember, order matters in the pipeline configuration.
```
    app.UseAuthentication();
    app.UseAuthorization();
```

## Add end-points to register and authenticate PortalUsers.
###Add an Account controller
See `Controllers\AccountController`. This uses DTO models for registration and login:
 * Models\RegisterRequest
 * Models\LoginRequest

#### Constructor Injection
Add UserManager and SignInManager through constructor injection to hook into Identity management. This provides access to password hashing, user creation, user sign-in etc.
```
    private readonly UserManager<PortalUser> _userManager;
    private readonly SignInManager<PortalUser> _signInManager;

    public AccountController(
        UserManager<PortalUser> userManager,
        SignInManager<PortalUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }
```

#### Create the registration end-point
Add a method and define a route for user registration. In this instance using HttpPost to submit a register (user creation) request. This creates a portal user via the UserManager which hashes the password etc.
```
    [HttpPost("portaluser")]
    public async Task<ActionResult> RegisterUser(RegisterRequest registerRequest)
    {
        var result = await _userManager.CreateAsync(registerRequest.PortalUser, registerRequest.Password);
            
        if (result.Succeeded)
        {
            return Ok();        // Should be CreatedAtRoute
        }

        return BadRequest("Invalid registrationg details");
    }
```

#### Create the token end-point
Add a method and define a route for authentication and token creation. In this instance using HttpPost to submit a login (token creation) request. This signs the user in with the details provided, and on success generates a JwtSecurityToken which is returned to the caller.
```
    [HttpPost("token")]
    public async Task<ActionResult<string>> GetAuthToken(LoginRequest loginRequest)
    {
        // Check user can sign in
        var result = await _signInManager.PasswordSignInAsync(
            loginRequest.Email,
            loginRequest.Password,
            false,
            false);

        if (result.Succeeded)
        {
            // Get the PortalUser
            var user = await _userManager.FindByNameAsync(loginRequest.Email);
            // Get expiry as seconds since epoch for the payload
            var expiry = (int)(DateTime.UtcNow.AddMinutes(60) - DateTime.UnixEpoch).TotalSeconds;
            // Build token header (See https://jwt.io/)
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("ShVmYq3s6v9y$B&E)H@McQfTjWnZr4u7"));
            var signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var header = new JwtHeader(signingCredentials);
            // Build token payload (See https://jwt.io/)
            var payload = new JwtPayload
            {
                { "sub", "api" },
                { "name", $"{user.UserName}" },
                { "exp", expiry },
                { "iss", "http://localhost" },
                { "aud", "http://localhost" }
            };
            // Build and return the token as a string
            var token = new JwtSecurityToken(header, payload);
            var handler = new JwtSecurityTokenHandler();
            var tokenString = handler.WriteToken(token);

            return Ok(tokenString);
        }

        return BadRequest("Invalid login details");
    }
```

Note: the "iss" and "aud" elements of the payload must match the ValidIssuer and ValidAudience configured in Startup.cs for authorization to work.

## Add Authorization to protect API end-points
The default example WeatherForecastController is included in this project. Add the [Authorize] attribute to the existing Get end-point.
```
    [HttpGet]
    [Authorize]
    public IEnumerable<WeatherForecast> Get()
    {
        ...
    }
```

## Test
F5 to run the project 

### Register a user
Using Postman or CURL submit a POST to https://localhost:44365/api/account/portaluser
The body of the request should include a JSON serialised RegisterRequest. e.g.
```
    {
        "PortalUser":{
            "UserName":"davidbrunning@useyournoodle.biz",
            "Email":"davidbrunning@useyournoodle.biz",
            "PhoneNumber":"+44 7xxx 6xxxxx"
        },
        "Password":"Passw0rd_",
        "ConfirmPassword":"Passw0rd_"
    }
```

If the user doesn't already exist, you will receive a 200 (OK) response. Check the AspNetUsers table in the database for the user details, including a hashed password.

### Get a security token
Using Postmand or CURL submit a POST to https://localhost:44365/api/account/token
The body of the request should include a JSON serialised LoginRequest. e.g.
```
    {
        "email":"davidbrunning@useyournoodle.biz",
        "password":"Passw0rd_"
    }
```

Having registered the user, you will receive a 200 (OK) response. The response body will contain the JWT bearer token. e.g.


### Submit an authenticated request
Using Postman or CURL submit a GET to https://localhost:44365/weatherforecast
Add an Authorization request header which contains the JWT bearer token to use .e.g.
```
Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJhcGkiLCJuYW1lIjoiZGF2aWRicnVubmluZ0B1c2V5b3Vybm9vZGxlLmJpeiIsImV4cCI6MTYxOTAyODcwOCwiaXNzIjoiaHR0cDovL2xvY2FsaG9zdCIsImF1ZCI6Imh0dHA6Ly9sb2NhbGhvc3QifQ.Mrw901sAJ0lS1e6daCLkCFK22gz0jSJx4xiqj1PGpCU
```

# Acknowledgements
Thanks to all of the following for useful information that helped pull this together:
 * https://stackoverflow.com/questions/65983243/use-net-core-identity-with-an-api
 * https://stackoverflow.com/questions/58165036/login-to-angular-spa-with-asp-net-core-3-api-without-redirect-outside-of-spa
 * https://jasonwatmore.com/post/2019/10/14/aspnet-core-3-simple-api-for-authentication-registration-and-user-management
 * https://youtu.be/h2hGGPHLqqc
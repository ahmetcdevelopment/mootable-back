using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Mootable.Application.Interfaces;
using Mootable.Infrastructure.Auth;
using Mootable.Infrastructure.Persistence;
using Mootable.Infrastructure.Persistence.Repositories;
using Mootable.Infrastructure.Services;

namespace Mootable.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());

        services.AddScoped<DbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());

        // Repository Pattern & Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        var jwtSettings = new JwtSettings();
        configuration.Bind(JwtSettings.SectionName, jwtSettings);
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddCookie(options =>
        {
            options.Cookie.Name = "MootableOAuth";
            options.LoginPath = "/api/oauth2/login";
            options.LogoutPath = "/api/oauth2/logout";
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtSettings.Secret)),
                ClockSkew = TimeSpan.Zero
            };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;
                    
                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    {
                        context.Token = accessToken;
                    }
                    
                    return Task.CompletedTask;
                }
            };
        });

        // Add OAuth2 providers with Cookie as SignIn scheme
        var googleClientId = configuration["OAuth2:Google:ClientId"];
        var googleClientSecret = configuration["OAuth2:Google:ClientSecret"];
        if (!string.IsNullOrEmpty(googleClientId) && googleClientId != "YOUR_GOOGLE_CLIENT_ID")
        {
            services.AddAuthentication()
                .AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
                {
                    options.ClientId = googleClientId;
                    options.ClientSecret = googleClientSecret!;
                    options.CallbackPath = "/signin-google";
                    options.SaveTokens = true;
                    options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.Scope.Add("email");
                    options.Scope.Add("profile");
                    
                    // Force the correct redirect URI
                    options.Events.OnRedirectToAuthorizationEndpoint = context =>
                    {
                        // Replace any incorrect redirect_uri with the correct one
                        var redirectUri = "http://localhost:5000/signin-google";
                        var uri = new Uri(context.RedirectUri);
                        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
                        query["redirect_uri"] = redirectUri;
                        
                        var newUri = $"{uri.GetLeftPart(UriPartial.Path)}?{query}";
                        context.Response.Redirect(newUri);
                        return Task.CompletedTask;
                    };
                });
        }

        var microsoftClientId = configuration["OAuth2:Microsoft:ClientId"];
        var microsoftClientSecret = configuration["OAuth2:Microsoft:ClientSecret"];
        if (!string.IsNullOrEmpty(microsoftClientId) && microsoftClientId != "YOUR_MICROSOFT_CLIENT_ID")
        {
            services.AddAuthentication()
                .AddMicrosoftAccount(MicrosoftAccountDefaults.AuthenticationScheme, options =>
                {
                    options.ClientId = microsoftClientId;
                    options.ClientSecret = microsoftClientSecret!;
                    options.CallbackPath = "/signin-microsoft";
                    options.SaveTokens = true;
                    options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                });
        }

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis");
            options.InstanceName = "Mootable:";
        });

        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IEmailService, EmailService>();

        return services;
    }
}

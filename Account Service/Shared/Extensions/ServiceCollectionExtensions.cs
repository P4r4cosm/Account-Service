using System.Reflection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace AccountService.Shared.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddSwaggerGetWithAuth(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Version = "v1",
                Title = "Account Service API",
                Description =
                    "Микросервис для управления банковскими счетами и транзакциями в соответствии с заданием Модуль Банка."
            });

            // Включаем отображение комментариев в интерфейсе Swagger
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);

            options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.OAuth2,
                Flows = new OpenApiOAuthFlows
                {
                    AuthorizationCode = new OpenApiOAuthFlow // Указываем, что используем Authorization Code Flow
                    {
                        // URL страницы логина в вашем Keycloak
                        AuthorizationUrl =
                            new Uri(configuration["Keycloak:AuthorizationUrl"] ?? string.Empty),

                        // URL для обмена кода на токен
                        TokenUrl =
                            new Uri(configuration["Keycloak:TokenUrl"] ?? string.Empty),

                        Scopes = new Dictionary<string, string>
                        {
                            { "openid", "OpenID Connect" },
                            { "profile", "User Profile" }
                        }
                    }
                }
            });
            // Указываем, что для вызова API требуется аутентификация по схеме "oauth2"
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "oauth2" }
                    },
                    ["openid", "profile"]
                }
            });
        });
    }

    public static void AddAuthenticationBearer(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddAuthentication("Bearer")
            .AddJwtBearer(o =>
            {
                o.RequireHttpsMetadata = false;
                o.Audience = configuration["Authentication:Audience"];
                o.Authority = configuration["Authentication:Authority"];
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = configuration["Authentication:ValidIssuer"]
                };
            });
    }
}
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using AgiVysSystem.Api.Data;
using AgiVysSystem.Api.Models.User;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using AgiVysSystem.Api.Interfaces;
using AgiVysSystem.Api.Service;
using Asp.Versioning;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.HttpOverrides;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. CONFIGURAÇÕES INICIAIS
// ==========================================

// Limpa mapeamento para não dar conflito com Claims curtas
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

builder.Services.AddHttpClient<AgiVysSystem.Api.Services.External.AsaasService>(client =>
{
    var baseUrl = builder.Configuration["Asaas:BaseUrl"] ?? "https://sandbox.asaas.com/api/v3/";
    var apiKey = builder.Configuration["Asaas:ApiKey"];

    client.BaseAddress = new Uri(baseUrl);
    client.DefaultRequestHeaders.Add("access_token", apiKey);
    client.DefaultRequestHeaders.Add("User-Agent", "AgiVysSystem-Api");
});

builder.Services.AddScoped<AgiVysSystem.Api.Services.Financial.CheckoutService>();

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.SuppressMapClientErrors = true;
    });
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "AgiVysSystem API", Version = "v1", Description = "API para gestão do ecossistema AgiVysSystem" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Cole APENAS o token JWT."
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new string[] {}
        }
    });

    c.OrderActionsBy(apiDesc => 
    {
        // Define um peso para cada controller. Menor número aparece primeiro.
        var tag = apiDesc.ActionDescriptor.RouteValues["controller"];
        return tag switch
        {
            "Auth" => "01",
            "AppSystem" => "02",
            "People" => "03",
            _ => "99" 
        };
    });

    c.MapType<Microsoft.AspNetCore.Mvc.ProblemDetails>(() => new OpenApiSchema { Type = "object", Nullable = true });
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

// Configuração para Proxy Reverso (Nginx/Subfolders)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Banco de Dados
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Identity
builder.Services.AddIdentity<User, IdentityRole<int>>(options => {
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// JWT Configuração
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"] ?? "");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,
        
        RoleClaimType = ClaimTypes.Role, 
        NameClaimType = ClaimTypes.NameIdentifier
    };

    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = context =>
        {
            var claimsIdentity = context.Principal.Identity as ClaimsIdentity;
            
            var roleClaim = claimsIdentity?.Claims.FirstOrDefault(c => 
                c.Type == "role" || c.Type == ClaimTypes.Role);

            if (roleClaim != null)
            {
                if (!claimsIdentity.HasClaim(ClaimTypes.Role, roleClaim.Value))
                {
                    claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, roleClaim.Value));
                }
            }
            else 
            {
                Console.WriteLine("\n[AVISO] Nenhuma role encontrada no Token!");
            }

            return Task.CompletedTask;
        }
    };
});

// ==========================================
// 2. INJEÇÕES DE DEPENDÊNCIA (RESTITUÍDAS)
// ==========================================

builder.Services.AddScoped<IEmailService, EmailService>();

builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
{
    options.TokenLifespan = TimeSpan.FromHours(2);
});

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// ==========================================
// CONFIGURAÇÃO RESTRITA DE CORS
// ==========================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("MainPolicy", policy =>
    {
        policy.WithOrigins(
                "http://localhost:4200", 
                "https://joederblanca.com.br"
              )
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.Configure<RouteOptions>(options => options.LowercaseUrls = true);

var app = builder.Build();

// ==========================================
// 3. PIPELINE (ORDEM DE EXECUÇÃO)
// ==========================================

// ATENÇÃO: Avisa a API que ela está rodando numa subpasta do domínio
// (Certifique-se de que o nome confere com a URL que o Angular está chamando)
app.UsePathBase("/agivys"); 

app.UseForwardedHeaders();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try {
        await AgiVysSystem.Api.Configuration.RoleSeeder.SeedRolesAsync(services);
    } catch (Exception ex) {
        Console.WriteLine($"Erro ao criar roles: {ex.Message}");
    }
}

if (app.Environment.IsDevelopment())
{
 
}

app.UseSwagger(c =>
{
    c.PreSerializeFilters.Add((swaggerDoc, httpReq) =>
    {
        if (httpReq.Headers.ContainsKey("X-Forwarded-Prefix"))
        {
            var prefix = httpReq.Headers["X-Forwarded-Prefix"];
            swaggerDoc.Servers = new List<OpenApiServer> 
            { 
                new OpenApiServer { Url = $"{httpReq.Scheme}://{httpReq.Host}{prefix}" } 
            };
        }
    });
});
app.UseSwaggerUI(c => 
{
    c.SwaggerEndpoint("v1/swagger.json", "AgiVysSystem API v1");
});

app.Urls.Add("http://0.0.0.0:5000");

// COMENTADO: Pois o NGINX já trata o certificado SSL na porta 443 
// app.UseHttpsRedirection();

app.UseCors("MainPolicy");

app.UseAuthentication(); 
app.UseAuthorization();  

app.MapControllers();

app.Run();
using Rotas;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Registra a autorização usada nas rotas que exigem login.
builder.Services.AddAuthorization();

// Configura a documentação da API e as respostas de erro.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();

// Faz os valores de enum aparecerem como texto no JSON.
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// Cadastro e login compartilham o limite de 10 requisições por minuto.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter("cadastro", limiterOptions =>
    {
        limiterOptions.PermitLimit = 10;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueLimit = 0;
    });
});

// Lê a chave JWT da configuração e impede o uso de uma chave muito curta.
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
{
    throw new InvalidOperationException("Jwt:Key deve existir e ter pelo menos 32 caracteres.");
}

var key = Encoding.UTF8.GetBytes(jwtKey);

// Define o JWT Bearer como o tipo de autenticação.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Confere a assinatura, quem emitiu o token, seu destinatário e sua validade.
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

// Configura o MySQL e verifica se a coluna de senha existe.
CreateConnection.ConnectionString = builder.Configuration.GetConnectionString("GestaoIncedentes")
    ?? string.Empty;
if (string.IsNullOrWhiteSpace(CreateConnection.ConnectionString))
{
    throw new InvalidOperationException("Configure ConnectionStrings:GestaoIncedentes antes de iniciar a API.");
}
CreateConnection.EnsurePasswordColumn();

var app = builder.Build();

// Define os passos pelos quais as requisições passam antes de chegar às rotas.
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseSwagger();
app.UseSwaggerUI();

// Registra cada rota do projeto.
app.cadastro();
app.login();
app.listarUsuarios();
app.criarChamado();
app.listarChamados();
app.buscarChamado();
app.atualizarStatus();
app.listarHistorico();
app.adicionarHistorico();

app.Run();

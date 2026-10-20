using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using MySql.Data.MySqlClient;

namespace Rotas;

public static class Rotas
{
    private static readonly string[] StatusValidos = ["Aberto", "Em andamento", "Resolvido", "Fechado"];
    private static readonly string[] PrioridadesValidas = ["Baixa", "Media", "Alta", "Critica"];

    public static void cadastro(this WebApplication app)
    {

        app.MapPost("/cadastro", (usuarios usuario) =>
        {
            if (string.IsNullOrWhiteSpace(usuario.nome) ||
                string.IsNullOrWhiteSpace(usuario.email) ||
                string.IsNullOrWhiteSpace(usuario.senha) || usuario.senha.Length < 6)
                return Results.BadRequest(new { erro = "Nome e email são obrigatórios." });
            try
            {
                using var connection = CreateConnection.GetConnection();
                connection.Open();
                using var cmd = new MySqlCommand("INSERT INTO usuarios (nome, email, departamento, senha) VALUES (@nome, @email, @departamento, @senha); SELECT LAST_INSERT_ID();", connection);
                cmd.Parameters.AddWithValue("@nome", usuario.nome.Trim());
                cmd.Parameters.AddWithValue("@email", usuario.email.Trim());
                cmd.Parameters.AddWithValue("@departamento", (object?)usuario.departamento?.Trim() ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@senha", CriarHashSenha(usuario.senha));
                var id = Convert.ToInt32(cmd.ExecuteScalar());
                return Results.Created($"/usuarios/{id}", new { idUsuario = id });
            }
            catch (MySqlException ex) when (ex.Number == 1062) { return Results.Conflict(new { erro = "Email já cadastrado." }); }
            catch (Exception ex) { Console.WriteLine("ERRO CADASTRO: " + ex.Message); return Results.Problem("Erro ao cadastrar usuário."); }
        }).RequireRateLimiting("cadastro");

    }

    public static void login(this WebApplication app)
    {
        app.MapPost("/login", (LoginRequest request, IConfiguration configuration) =>
        {
            if (string.IsNullOrWhiteSpace(request.email) || string.IsNullOrWhiteSpace(request.senha))
                return Results.BadRequest(new { erro = "Email e senha são obrigatórios." });

            using var connection = CreateConnection.GetConnection();
            connection.Open();
            using var command = new MySqlCommand(
                "SELECT idUsuario, nome, email, senha FROM usuarios WHERE email = @email LIMIT 1", connection);
            command.Parameters.AddWithValue("@email", request.email.Trim());
            using var reader = command.ExecuteReader();
            if (!reader.Read() || reader.IsDBNull(reader.GetOrdinal("senha")) ||
                !ValidarSenha(request.senha, reader.GetString("senha")))
                return Results.Unauthorized();

            var id = reader.GetInt32("idUsuario");
            var nome = reader.IsDBNull(reader.GetOrdinal("nome")) ? request.email : reader.GetString("nome");
            var token = CriarToken(id, nome, request.email, configuration);
            return Results.Ok(new { token, tipo = "Bearer", expiracaoMinutos = configuration.GetValue<int>("Jwt:ExpirationMinutes", 120) });
        }).RequireRateLimiting("cadastro");

    }

    public static void listarUsuarios(this WebApplication app)
    {
        app.MapGet("/usuarios", () =>
        {
            using var connection = CreateConnection.GetConnection();
            connection.Open();
            using var cmd = new MySqlCommand("SELECT idUsuario, nome, email, departamento FROM usuarios ORDER BY nome", connection);
            using var reader = cmd.ExecuteReader();
            var lista = new List<object>();
            while (reader.Read()) lista.Add(new
            {
                idUsuario = reader.GetInt32("idUsuario"),
                nome = reader.IsDBNull(reader.GetOrdinal("nome")) ? null : reader.GetString("nome"),
                email = reader.IsDBNull(reader.GetOrdinal("email")) ? null : reader.GetString("email"),
                departamento = reader.IsDBNull(reader.GetOrdinal("departamento")) ? null : reader.GetString("departamento")
            });
            return Results.Ok(lista);
        }).RequireAuthorization();

    }

    public static void criarChamado(this WebApplication app)
    {
        app.MapPost("/chamados", (CriarChamadoRequest request) =>
        {
            var erro = ValidarChamado(request.titulo, request.descricao, request.chamadoStatus, request.prioridade);
            if (erro is not null) return Results.BadRequest(new { erro });
            try
            {
                using var connection = CreateConnection.GetConnection();
                connection.Open();
                using var cmd = new MySqlCommand(@"INSERT INTO chamados
                    (titulo, descricao, tipo, chamadoStatus, prioridade, dataAbertura, usuario_id)
                    VALUES (@titulo, @descricao, @tipo, @status, @prioridade, @data, @usuario); SELECT LAST_INSERT_ID();", connection);
                cmd.Parameters.AddWithValue("@titulo", request.titulo.Trim());
                cmd.Parameters.AddWithValue("@descricao", request.descricao.Trim());
                cmd.Parameters.AddWithValue("@tipo", (object?)request.tipo?.Trim() ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@status", request.chamadoStatus ?? "Aberto");
                cmd.Parameters.AddWithValue("@prioridade", request.prioridade ?? "Baixa");
                cmd.Parameters.AddWithValue("@data", request.dataAbertura?.Date ?? DateTime.Today);
                cmd.Parameters.AddWithValue("@usuario", (object?)request.usuario_id ?? DBNull.Value);
                var id = Convert.ToInt32(cmd.ExecuteScalar());
                return Results.Created($"/chamados/{id}", new { idChamado = id });
            }
            catch (MySqlException ex) when (ex.Number == 1452) { return Results.BadRequest(new { erro = "O usuário informado não existe." }); }
        }).RequireAuthorization();

    }

    public static void listarChamados(this WebApplication app)
    {
        app.MapGet("/chamados", () =>
        {
            using var connection = CreateConnection.GetConnection();
            connection.Open();
            using var cmd = new MySqlCommand(@"SELECT c.idChamado, c.titulo, c.descricao, c.tipo, c.chamadoStatus,
                c.prioridade, c.dataAbertura, c.usuario_id, u.nome AS usuario_nome
                FROM chamados c LEFT JOIN usuarios u ON u.idUsuario = c.usuario_id ORDER BY c.idChamado DESC", connection);
            using var reader = cmd.ExecuteReader();
            var lista = new List<object>();
            while (reader.Read()) lista.Add(MapChamado(reader));
            return Results.Ok(lista);
        }).RequireAuthorization();

    }

    public static void buscarChamado(this WebApplication app)
    {
        app.MapGet("/chamados/{id:int}", (int id) =>
        {
            using var connection = CreateConnection.GetConnection();
            connection.Open();
            using var cmd = new MySqlCommand(@"SELECT c.idChamado, c.titulo, c.descricao, c.tipo, c.chamadoStatus,
                c.prioridade, c.dataAbertura, c.usuario_id, u.nome AS usuario_nome
                FROM chamados c LEFT JOIN usuarios u ON u.idUsuario = c.usuario_id WHERE c.idChamado = @id", connection);
            cmd.Parameters.AddWithValue("@id", id);
            using var reader = cmd.ExecuteReader();
            return reader.Read() ? Results.Ok(MapChamado(reader)) : Results.NotFound();
        }).RequireAuthorization();

    }

    public static void atualizarStatus(this WebApplication app)
    {
        app.MapPut("/chamados/{id:int}/status", (int id, AtualizarStatusRequest request) =>
        {
            if (string.IsNullOrWhiteSpace(request.status) || !StatusValidos.Contains(request.status))
                return Results.BadRequest(new { erro = "Status inválido." });
            using var connection = CreateConnection.GetConnection();
            connection.Open();
            using var cmd = new MySqlCommand("UPDATE chamados SET chamadoStatus = @status WHERE idChamado = @id", connection);
            cmd.Parameters.AddWithValue("@status", request.status);
            cmd.Parameters.AddWithValue("@id", id);
            return cmd.ExecuteNonQuery() == 0 ? Results.NotFound() : Results.Ok(new { idChamado = id, chamadoStatus = request.status });
        }).RequireAuthorization();

    }

    public static void listarHistorico(this WebApplication app)
    {
        app.MapGet("/chamados/{id:int}/historico", (int id) =>
        {
            using var connection = CreateConnection.GetConnection();
            connection.Open();
            using var cmd = new MySqlCommand(@"SELECT h.idHistorico, h.chamado_id, h.descricao, h.responsavel_id,
                h.data_evento, u.nome AS responsavel_nome FROM historico h
                LEFT JOIN usuarios u ON u.idUsuario = h.responsavel_id
                WHERE h.chamado_id = @id ORDER BY h.data_evento, h.idHistorico", connection);
            cmd.Parameters.AddWithValue("@id", id);
            using var reader = cmd.ExecuteReader();
            var lista = new List<object>();
            while (reader.Read()) lista.Add(new
            {
                idHistorico = reader.GetInt32("idHistorico"),
                chamado_id = reader.GetInt32("chamado_id"),
                descricao = reader.GetString("descricao"),
                responsavel_id = reader.IsDBNull(reader.GetOrdinal("responsavel_id")) ? (int?)null : reader.GetInt32("responsavel_id"),
                responsavel_nome = reader.IsDBNull(reader.GetOrdinal("responsavel_nome")) ? null : reader.GetString("responsavel_nome"),
                data_evento = reader.IsDBNull(reader.GetOrdinal("data_evento")) ? (DateTime?)null : reader.GetDateTime("data_evento")
            });
            return Results.Ok(lista);
        }).RequireAuthorization();

    }

    public static void adicionarHistorico(this WebApplication app)
    {
        app.MapPost("/chamados/{id:int}/historico", (int id, AdicionarHistoricoRequest request) =>
        {
            if (string.IsNullOrWhiteSpace(request.descricao) || request.descricao.Length > 300)
                return Results.BadRequest(new { erro = "A descrição é obrigatória e deve ter até 300 caracteres." });
            try
            {
                using var connection = CreateConnection.GetConnection();
                connection.Open();
                using var cmd = new MySqlCommand(@"INSERT INTO historico
                    (chamado_id, descricao, responsavel_id, data_evento)
                    VALUES (@chamado, @descricao, @responsavel, @data); SELECT LAST_INSERT_ID();", connection);
                cmd.Parameters.AddWithValue("@chamado", id);
                cmd.Parameters.AddWithValue("@descricao", request.descricao.Trim());
                cmd.Parameters.AddWithValue("@responsavel", (object?)request.responsavel_id ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@data", request.data_evento?.Date ?? DateTime.Today);
                var historicoId = Convert.ToInt32(cmd.ExecuteScalar());
                return Results.Created($"/chamados/{id}/historico", new { idHistorico = historicoId, chamado_id = id });
            }
            catch (MySqlException ex) when (ex.Number == 1452) { return Results.BadRequest(new { erro = "Chamado ou responsável não encontrado." }); }
        }).RequireAuthorization();
    }

    private static string? ValidarChamado(string titulo, string descricao, string? status, string? prioridade)
    {
        if (string.IsNullOrWhiteSpace(titulo) || titulo.Length > 100) return "Título é obrigatório e deve ter até 100 caracteres.";
        if (string.IsNullOrWhiteSpace(descricao) || descricao.Length > 300) return "Descrição é obrigatória e deve ter até 300 caracteres.";
        if (status is not null && !StatusValidos.Contains(status)) return "Status inválido.";
        if (prioridade is not null && !PrioridadesValidas.Contains(prioridade)) return "Prioridade inválida.";
        return null;
    }

    private static object MapChamado(MySqlDataReader reader) => new
    {
        idChamado = reader.GetInt32("idChamado"), titulo = reader.GetString("titulo"), descricao = reader.GetString("descricao"),
        tipo = reader.IsDBNull(reader.GetOrdinal("tipo")) ? null : reader.GetString("tipo"), chamadoStatus = reader.GetString("chamadoStatus"),
        prioridade = reader.GetString("prioridade"), dataAbertura = reader.IsDBNull(reader.GetOrdinal("dataAbertura")) ? (DateTime?)null : reader.GetDateTime("dataAbertura"),
        usuario_id = reader.IsDBNull(reader.GetOrdinal("usuario_id")) ? (int?)null : reader.GetInt32("usuario_id"),
        usuario_nome = reader.IsDBNull(reader.GetOrdinal("usuario_nome")) ? null : reader.GetString("usuario_nome")
    };

    private static string CriarHashSenha(string senha)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(senha, salt, 100_000, HashAlgorithmName.SHA256, 32);
        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    private static bool ValidarSenha(string senha, string senhaArmazenada)
    {
        var partes = senhaArmazenada.Split('.', 2);
        if (partes.Length != 2) return false;
        try
        {
            var salt = Convert.FromBase64String(partes[0]);
            var esperado = Convert.FromBase64String(partes[1]);
            var atual = Rfc2898DeriveBytes.Pbkdf2(senha, salt, 100_000, HashAlgorithmName.SHA256, esperado.Length);
            return CryptographicOperations.FixedTimeEquals(atual, esperado);
        }
        catch (FormatException) { return false; }
    }

    private static string CriarToken(int id, string nome, string email, IConfiguration configuration)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, id.ToString()),
            new Claim(ClaimTypes.Name, nome),
            new Claim(ClaimTypes.Email, email)
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(configuration.GetValue<int>("Jwt:ExpirationMinutes", 120));
        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public sealed record LoginRequest(string email, string senha);

public sealed record CriarChamadoRequest
{
    public string titulo { get; init; } = string.Empty;
    public string descricao { get; init; } = string.Empty;
    public string? tipo { get; init; }
    public string? chamadoStatus { get; init; }
    public string? prioridade { get; init; }
    public DateTime? dataAbertura { get; init; }
    public int? usuario_id { get; init; }
}

public sealed record AtualizarStatusRequest(string status);

public sealed record AdicionarHistoricoRequest
{
    public string descricao { get; init; } = string.Empty;
    public int? responsavel_id { get; init; }
    public DateTime? data_evento { get; init; }
}

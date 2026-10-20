using MySql.Data.MySqlClient;

public static class CreateConnection
{
    public static string ConnectionString { get; set; } = string.Empty;

    public static MySqlConnection GetConnection() => new(ConnectionString);

    public static void EnsurePasswordColumn()
    {
        try
        {
            using var connection = CreateConnection.GetConnection();
            connection.Open();
            using var command = new MySqlCommand(
                "ALTER TABLE usuarios ADD COLUMN senha VARCHAR(255) NULL", connection);
            command.ExecuteNonQuery();
        }
        catch (MySqlException ex) when (ex.Number == 1060)
        {
            // A coluna já existe.
        }
        catch (MySqlException ex)
        {
            Console.WriteLine($"Aviso: não foi possível validar a coluna senha: {ex.Message}");
        }
    }
}

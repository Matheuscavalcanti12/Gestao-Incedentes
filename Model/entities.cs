
public class usuarios
{
    public int idUsuario { get; set; }
    public string? nome { get; set; }
    public string? email { get; set; }
    public string? departamento { get; set; }
    public string? senha { get; set; }
}

public class chamados
{
    public int idChamado { get; set; }
    public string titulo { get; set; } = string.Empty;
    public string descricao { get; set; } = string.Empty;
    public string? tipo { get; set; }
    public string chamadoStatus { get; set; } = "Aberto";
    public string prioridade { get; set; } = "Baixa";
    public DateTime? dataAbertura { get; set; }

    public int? usuario_id { get; set; }

    public usuarios? Usuario { get; set; }
}

public class historico
{
    public int idHistorico { get; set; }
    public int chamado_id { get; set; }
    public string descricao { get; set; } = string.Empty;
    public DateTime? data_evento { get; set; }

    public int? responsavel_id { get; set; }

    public chamados? Chamado { get; set; }
    public usuarios? Usuario { get; set; }
}

public class mudancas
{
    public int id { get; set; }
    public int? chamado_id { get; set; }

    public string? risco { get; set; }
    public DateTime? data_planejada { get; set; }
    public string? status_aprovacao { get; set; }

    public int? aprovador_id { get; set; }

    public chamados? Chamado { get; set; }
    public usuarios? Usuario { get; set; }
}

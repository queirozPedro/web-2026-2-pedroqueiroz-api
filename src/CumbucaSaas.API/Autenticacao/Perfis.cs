namespace CumbucaSaas.API.Autenticacao;

/// <summary>Nomes das policies de autorização e das claims que a API acrescenta ao token.</summary>
public static class Perfis
{
    /// <summary>Policy: e-mail @ufersa.edu.br (ou na lista de funcionários da configuração).</summary>
    public const string Funcionario = "funcionario";

    /// <summary>Policy: e-mail @alunos.ufersa.edu.br.</summary>
    public const string Estudante = "estudante";

    /// <summary>Id do Usuario no banco.</summary>
    public const string ClaimUsuarioId = "cumbuca_usuario_id";

    /// <summary>Id do Estudante ligado ao usuário (só para estudantes já cadastrados no RU).</summary>
    public const string ClaimEstudanteId = "cumbuca_estudante_id";

    /// <summary>AuthenticationType da identidade que a API acrescenta (marca que o acesso já foi registrado).</summary>
    public const string TipoIdentidade = "cumbuca";
}

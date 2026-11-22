namespace CumbucaSaas.API.Autenticacao;

/// <summary>Nomes das policies de autorização e das claims que a API acrescenta ao token.</summary>
public static class Perfis
{
    /// <summary>Policy: login com e-mail e senha (conta criada pelo cadastro), com cadastro concluído.</summary>
    public const string Funcionario = "funcionario";

    /// <summary>Policy: login com Google institucional (@...ufersa.edu.br), com cadastro concluído.</summary>
    public const string Estudante = "estudante";

    /// <summary>Policy: só exige o token válido (rotas do próprio usuário, como /api/usuarios/me).</summary>
    public const string Logado = "logado";

    /// <summary>Policy: tem perfil, mesmo com o cadastro incompleto (para concluir o cadastro).</summary>
    public const string ComPerfil = "com-perfil";

    /// <summary>Id do Usuario no banco.</summary>
    public const string ClaimUsuarioId = "cumbuca_usuario_id";

    /// <summary>Id do Estudante ligado ao usuário (só para estudantes já cadastrados no RU).</summary>
    public const string ClaimEstudanteId = "cumbuca_estudante_id";

    /// <summary>"true" quando o usuário já informou nome completo e data de nascimento.</summary>
    public const string ClaimCadastroCompleto = "cumbuca_cadastro_completo";

    /// <summary>Por que o usuário ficou sem perfil (ver <see cref="MotivosSemAcesso"/>).</summary>
    public const string ClaimMotivoSemAcesso = "cumbuca_motivo_sem_acesso";

    /// <summary>AuthenticationType da identidade que a API acrescenta (marca que o acesso já foi registrado).</summary>
    public const string TipoIdentidade = "cumbuca";
}

/// <summary>Valores da claim <see cref="Perfis.ClaimMotivoSemAcesso"/>, repassados ao front em /api/usuarios/me.</summary>
public static class MotivosSemAcesso
{
    /// <summary>Login com Google usando e-mail fora de ufersa.edu.br.</summary>
    public const string DominioNaoPermitido = "dominio-nao-permitido";

    /// <summary>O e-mail já pertence a outra conta (a outra forma de login).</summary>
    public const string EmailEmUso = "email-em-uso";
}

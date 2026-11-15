namespace CumbucaSaas.Infrastructure;

/// <summary>
/// Usuário logado na requisição atual, para a auditoria. A implementação fica na
/// API (lê o token); fora de uma requisição (migrations, por exemplo) devolve nulo.
/// </summary>
public interface IUsuarioAtual
{
    int? UsuarioId { get; }
}

using FluentResults;

namespace CumbucaSaas.API.Commands;

/// <summary>O recurso alvo do command não existe (vira 404 no controller).</summary>
public sealed class NaoEncontradoError(string mensagem) : Error(mensagem);

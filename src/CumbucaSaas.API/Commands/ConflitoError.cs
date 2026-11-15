using FluentResults;

namespace CumbucaSaas.API.Commands;

/// <summary>O command bate com o estado atual dos dados (ex.: registro duplicado); vira 409 no controller.</summary>
public sealed class ConflitoError(string mensagem) : Error(mensagem);

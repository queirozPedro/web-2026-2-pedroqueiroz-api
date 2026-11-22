using CumbucaSaas.Domain.Aggregates.UsuarioAggregate;
using FluentValidation;

namespace CumbucaSaas.API.Commands;

/// <summary>Regras dos dados do usuário, iguais no cadastro e na conclusão do cadastro (e no front).</summary>
internal static class RegrasUsuario
{
    public const int SenhaTamanhoMinimo = 8;
    public const int SenhaTamanhoMaximo = 256;

    public static IRuleBuilderOptions<T, string> NomeCompleto<T>(this IRuleBuilder<T, string> regra) =>
        regra
            .Must(Usuario.NomeCompletoValido)
            .WithMessage(
                $"Informe o nome completo: só letras, com pelo menos {Usuario.PalavrasMinimasNome} palavras de " +
                $"{Usuario.LetrasMinimasPorPalavra} letras ou mais (ex.: Maria Silva).");

    public static IRuleBuilderOptions<T, DateOnly> DataNascimento<T>(this IRuleBuilder<T, DateOnly> regra) =>
        regra
            .Must(data => Usuario.DataNascimentoValida(data, HoraLocal.Hoje))
            .WithMessage(
                $"Informe uma data de nascimento entre {Usuario.DataNascimentoMinima:dd/MM/yyyy} e hoje.");

    public static IRuleBuilderOptions<T, string> Email<T>(this IRuleBuilder<T, string> regra) =>
        regra
            .Must(Usuario.EmailValido)
            .WithMessage("Informe um e-mail válido (ex.: nome@dominio.com).");

    /// <summary>A política padrão do User Pool do Cognito.</summary>
    public static IRuleBuilderOptions<T, string> Senha<T>(this IRuleBuilder<T, string> regra) =>
        regra
            .Must(senha => !string.IsNullOrEmpty(senha)
                           && senha.Length is >= SenhaTamanhoMinimo and <= SenhaTamanhoMaximo
                           && senha.Any(char.IsUpper)
                           && senha.Any(char.IsLower)
                           && senha.Any(char.IsDigit)
                           && senha.Any(c => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c))
                           && senha.Trim() == senha)
            .WithMessage(
                $"A senha precisa de pelo menos {SenhaTamanhoMinimo} caracteres, com letra maiúscula, " +
                "letra minúscula, número e símbolo, sem espaço no início ou no fim.");
}

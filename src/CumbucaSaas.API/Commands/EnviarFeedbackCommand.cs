using CumbucaSaas.API.Behaviors;
using CumbucaSaas.API.TempoReal;
using CumbucaSaas.Domain.Aggregates.EstudanteAggregate;
using CumbucaSaas.Domain.Aggregates.FeedbackAggregate;
using CumbucaSaas.Domain.SeedWork;
using FluentResults;
using FluentValidation;
using MediatR;

namespace CumbucaSaas.API.Commands;

/// <summary>
/// Estudante envia um feedback: assunto, comentário e nota de 1 a 5 para cada uma
/// das categorias de avaliação. A data é a de hoje (Brasília). Devolve o Id criado.
/// O EstudanteId vem do token do estudante logado (FeedbacksController), nunca do corpo.
/// </summary>
public sealed record EnviarFeedbackCommand(
    int EstudanteId,
    int CategoriaFeedbackId,
    string Comentario,
    IReadOnlyDictionary<string, int> Notas) : IRequest<Result<int>>;

public sealed class EnviarFeedbackCommandValidator : AbstractValidator<EnviarFeedbackCommand>
{
    public EnviarFeedbackCommandValidator()
    {
        RuleFor(c => c.EstudanteId).GreaterThan(0);
        RuleFor(c => c.CategoriaFeedbackId).GreaterThan(0);
        RuleFor(c => c.Comentario).NotEmpty().MaximumLength(Feedback.ComentarioTamanhoMaximo);

        var categorias = Enumeration.GetAll<CategoriaAvaliacao>().Select(c => c.Name).ToList();

        // Stop: sem notas, nem tenta conferir as categorias (evita NullReference no Must).
        RuleFor(c => c.Notas)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .Must(notas => categorias.All(nome => notas.Keys.Any(k => string.Equals(k, nome, StringComparison.OrdinalIgnoreCase)))
                           && notas.Count == categorias.Count)
            .WithMessage($"Informe uma nota para cada categoria: {string.Join(", ", categorias)}.");

        RuleForEach(c => c.Notas)
            .Must(par => par.Value is >= Avaliacao.NotaMinima and <= Avaliacao.NotaMaxima)
            .WithMessage(
                (_, par) => $"A nota de '{par.Key}' deve ficar entre {Avaliacao.NotaMinima} e {Avaliacao.NotaMaxima}.");
    }
}

public sealed class EnviarFeedbackCommandHandler(
    IFeedbackRepository feedbacks,
    IEstudanteRepository estudantes,
    INotificadorTempoReal notificador)
    : IRequestHandler<EnviarFeedbackCommand, Result<int>>
{
    public async Task<Result<int>> Handle(EnviarFeedbackCommand command, CancellationToken cancellationToken)
    {
        var estudante = await estudantes.ObterAsync(command.EstudanteId, cancellationToken);
        if (estudante is null)
        {
            return Result.Fail<int>(new NaoEncontradoError($"Estudante {command.EstudanteId} não encontrado."));
        }

        if (!estudante.EstaAtivo)
        {
            return Result.Fail<int>(new ConflitoError("Estudante com vínculo inativo não pode enviar feedback."));
        }

        if (!await feedbacks.ExisteCategoriaAsync(command.CategoriaFeedbackId, cancellationToken))
        {
            return Result.Fail<int>(new ValidacaoError(
                nameof(command.CategoriaFeedbackId),
                $"Categoria de feedback {command.CategoriaFeedbackId} não existe. Veja GET /api/categorias-feedback."));
        }

        // O validator já garantiu nomes válidos (sem diferenciar maiúsculas) e uma nota por categoria.
        var notas = command.Notas.ToDictionary(
            par => Enumeration.FromName<CategoriaAvaliacao>(par.Key),
            par => par.Value);

        var feedback = feedbacks.Adicionar(Feedback.Enviar(
            estudante.Id, HoraLocal.Hoje, command.CategoriaFeedbackId, command.Comentario, notas));
        await feedbacks.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        await notificador.NotificarAsync("dashboard-atualizado", cancellationToken);

        return Result.Ok(feedback.Id);
    }
}

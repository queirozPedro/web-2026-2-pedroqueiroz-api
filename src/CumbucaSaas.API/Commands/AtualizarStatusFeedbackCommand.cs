using CumbucaSaas.Domain.Aggregates.FeedbackAggregate;
using CumbucaSaas.Domain.SeedWork;
using FluentResults;
using FluentValidation;
using MediatR;

namespace CumbucaSaas.API.Commands;

/// <summary>Funcionário muda o status de um feedback (novo, em_analise, resolvido).</summary>
public sealed record AtualizarStatusFeedbackCommand(int FeedbackId, string Status) : IRequest<Result<Unit>>;

public sealed class AtualizarStatusFeedbackCommandValidator : AbstractValidator<AtualizarStatusFeedbackCommand>
{
    public AtualizarStatusFeedbackCommandValidator()
    {
        RuleFor(c => c.FeedbackId).GreaterThan(0);

        RuleFor(c => c.Status)
            .NotEmpty()
            .Must(status => Enumeration.TryFromName<StatusFeedback>(status, out _))
            .WithMessage(c =>
                $"Status '{c.Status}' inválido. Use: {string.Join(", ", Enumeration.GetAll<StatusFeedback>())}.");
    }
}

public sealed class AtualizarStatusFeedbackCommandHandler(IFeedbackRepository feedbacks)
    : IRequestHandler<AtualizarStatusFeedbackCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(AtualizarStatusFeedbackCommand command, CancellationToken cancellationToken)
    {
        var feedback = await feedbacks.ObterAsync(command.FeedbackId, cancellationToken);
        if (feedback is null)
        {
            return Result.Fail<Unit>(new NaoEncontradoError($"Feedback {command.FeedbackId} não encontrado."));
        }

        feedback.AlterarStatus(Enumeration.FromName<StatusFeedback>(command.Status));
        await feedbacks.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        return Result.Ok(Unit.Value);
    }
}

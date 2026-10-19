using CumbucaSaas.Domain.Aggregates.FeedbackAggregate;
using MediatR;

namespace CumbucaSaas.Domain.Events;

public sealed record StatusFeedbackAlteradoDomainEvent(
    int FeedbackId,
    StatusFeedback StatusAnterior,
    StatusFeedback NovoStatus) : INotification;

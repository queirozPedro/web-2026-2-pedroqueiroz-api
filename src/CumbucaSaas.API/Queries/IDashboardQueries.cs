namespace CumbucaSaas.API.Queries;

public interface IDashboardQueries
{
    /// <summary>Registros por turno e feedbacks, em ordem cronológica. Sem período, devolve todo o histórico.</summary>
    Task<DashboardViewModel> ObterAsync(DateOnly? inicio, DateOnly? fim, CancellationToken cancellationToken = default);
}

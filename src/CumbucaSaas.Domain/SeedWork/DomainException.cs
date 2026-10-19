namespace CumbucaSaas.Domain.SeedWork;

/// <summary>Violação de uma regra de negócio do domínio.</summary>
public class DomainException(string message) : Exception(message);

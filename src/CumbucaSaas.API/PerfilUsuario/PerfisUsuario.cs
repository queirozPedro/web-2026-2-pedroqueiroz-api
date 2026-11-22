using System.Globalization;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Options;

namespace CumbucaSaas.API.PerfilUsuario;

/// <summary>Um item da tabela de perfis no DynamoDB (um por usuário).</summary>
/// <param name="Id">"sub" do Cognito (chave de partição).</param>
/// <param name="FotoChave">Caminho da foto no bucket; nulo enquanto não houver foto.</param>
public sealed record PerfilSalvo(
    string Id,
    int TenantId,
    string Email,
    string Nome,
    DateOnly DataNascimento,
    string Perfil,
    string? FotoChave,
    DateTime AtualizadoEmUtc);

public interface IPerfisUsuario
{
    /// <summary>Nulo se o usuário ainda não salvou o perfil ou se o item é de outro tenant.</summary>
    Task<PerfilSalvo?> ObterAsync(string id, int tenantId, CancellationToken cancellationToken = default);

    Task SalvarAsync(PerfilSalvo perfil, CancellationToken cancellationToken = default);
}

/// <summary>
/// Tabela do DynamoDB com chave de partição "id" (String). Os atributos ficam com os
/// mesmos nomes do JSON da API (email, nome, dataNascimento...), para ler fácil no console.
/// </summary>
internal sealed class DynamoPerfisUsuario(IAmazonDynamoDB dynamo, IOptions<PerfilOptions> opcoes) : IPerfisUsuario
{
    private const string FormatoData = "yyyy-MM-dd";

    private string Tabela => opcoes.Value.Tabela;

    public async Task<PerfilSalvo?> ObterAsync(string id, int tenantId, CancellationToken cancellationToken = default)
    {
        var resposta = await dynamo.GetItemAsync(
            new GetItemRequest
            {
                TableName = Tabela,
                Key = new Dictionary<string, AttributeValue> { ["id"] = new() { S = id } },
                ConsistentRead = true
            },
            cancellationToken);

        // No AWSSDK v4 o Item vem nulo quando a chave não existe.
        var item = resposta.Item;
        if (item is null || item.Count == 0)
        {
            return null;
        }

        var perfil = new PerfilSalvo(
            item["id"].S,
            int.Parse(item["tenantId"].N, CultureInfo.InvariantCulture),
            item["email"].S,
            item["nome"].S,
            DateOnly.ParseExact(item["dataNascimento"].S, FormatoData, CultureInfo.InvariantCulture),
            item["perfil"].S,
            item.TryGetValue("fotoChave", out var foto) ? foto.S : null,
            DateTime.Parse(item["atualizadoEm"].S, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));

        return perfil.TenantId == tenantId ? perfil : null;
    }

    public Task SalvarAsync(PerfilSalvo perfil, CancellationToken cancellationToken = default)
    {
        var item = new Dictionary<string, AttributeValue>
        {
            ["id"] = new() { S = perfil.Id },
            ["tenantId"] = new() { N = perfil.TenantId.ToString(CultureInfo.InvariantCulture) },
            ["email"] = new() { S = perfil.Email },
            ["nome"] = new() { S = perfil.Nome },
            ["dataNascimento"] = new() { S = perfil.DataNascimento.ToString(FormatoData, CultureInfo.InvariantCulture) },
            ["perfil"] = new() { S = perfil.Perfil },
            ["atualizadoEm"] = new() { S = perfil.AtualizadoEmUtc.ToString("O", CultureInfo.InvariantCulture) }
        };

        if (perfil.FotoChave is not null)
        {
            item["fotoChave"] = new() { S = perfil.FotoChave };
        }

        return dynamo.PutItemAsync(new PutItemRequest { TableName = Tabela, Item = item }, cancellationToken);
    }
}

/*
 * Lambda da WebSocket API do Cumbuca (Node.js, editável direto no console).
 * Rotas: $connect, $disconnect, $default e broadcast, todas apontando para esta função.
 * Variável de ambiente: TABELA_CONEXOES (tabela do DynamoDB com partition key "connectionId", String).
 *
 * Quem envia {"action":"broadcast","mensagem":{...}} faz a "mensagem" chegar em
 * todas as conexões abertas. A CumbucaApi usa isso para avisar o front.
 */
import { DynamoDBClient, PutItemCommand, DeleteItemCommand, ScanCommand } from "@aws-sdk/client-dynamodb";
import {
  ApiGatewayManagementApiClient,
  PostToConnectionCommand,
  GoneException
} from "@aws-sdk/client-apigatewaymanagementapi";

const TABELA = process.env.TABELA_CONEXOES;
const db = new DynamoDBClient({});

const salvar = (id) =>
  db.send(new PutItemCommand({ TableName: TABELA, Item: { connectionId: { S: id } } }));

const remover = (id) =>
  db.send(new DeleteItemCommand({ TableName: TABELA, Key: { connectionId: { S: id } } }));

export const handler = async (event) => {
  const { routeKey, connectionId, domainName, stage } = event.requestContext;
  console.log(routeKey, connectionId);

  switch (routeKey) {
    case "$connect":
      await salvar(connectionId);
      return { statusCode: 200 };

    case "$disconnect":
      await remover(connectionId);
      return { statusCode: 200 };

    case "broadcast": {
      const { mensagem } = JSON.parse(event.body ?? "{}");
      const api = new ApiGatewayManagementApiClient({ endpoint: `https://${domainName}/${stage}` });
      const { Items = [] } = await db.send(new ScanCommand({ TableName: TABELA }));

      await Promise.all(
        Items.map(async (item) => {
          const id = item.connectionId.S;
          try {
            await api.send(new PostToConnectionCommand({ ConnectionId: id, Data: JSON.stringify(mensagem) }));
          } catch (erro) {
            // Conexão que caiu sem passar pelo $disconnect.
            if (erro instanceof GoneException) await remover(id);
            else throw erro;
          }
        })
      );
      return { statusCode: 200 };
    }

    default:
      return { statusCode: 200 };
  }
};

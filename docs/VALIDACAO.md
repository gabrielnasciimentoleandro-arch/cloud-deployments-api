# Relatório de validação

Revisão local executada em **28 de setembro de 2026**.

## Ambiente

- .NET SDK: `8.0.425`
- ASP.NET Core Runtime: `8.0.31`
- Bicep CLI: `0.47.16`
- Hadolint: `2.15.1`
- actionlint: `1.7.12`
- ShellCheck: `0.11.0`

## Resultados automatizados

| Verificação | Resultado |
|---|---:|
| Build Release | aprovado, 0 erros e 0 avisos |
| Testes automatizados | 18 aprovados, 0 falhas |
| Cobertura de linhas | 100% |
| Cobertura de branches | 100% |
| Formatação do código | aprovada |
| Pacotes vulneráveis | nenhum encontrado |
| Pacotes obsoletos | nenhum encontrado |
| OpenAPI 3.0 | válido |
| Bicep | compilado para ARM JSON com sucesso |
| Dockerfile | aprovado pelo Hadolint |
| GitHub Actions | aprovado pelo actionlint |
| Scripts Bash | aprovados pelo ShellCheck |
| Docker Compose | válido contra a especificação Compose |

## Teste com o host real

A API foi iniciada em modo `Production` com Kestrel e validada por requisições HTTP reais. Foram aprovados **16 de 16 grupos de verificações**.

Cenários aprovados:

- informações do serviço;
- endpoint de saúde;
- documento Swagger gerado com todas as rotas;
- criação de deployment;
- consulta por identificador;
- atualização do status;
- listagem com filtros;
- rejeição de campos inválidos, propriedades desconhecidas, enum numérico e JSON malformado com HTTP 400;
- exclusão com HTTP 204 e consulta posterior com HTTP 404;
- 30 de 30 health checks concorrentes;
- retorno HTTP 429 depois de exceder o limite configurado;
- endpoint de saúde disponível mesmo após o limite da API ser atingido.

## Validação do container

O ambiente de revisão não possuía um daemon Docker. Por isso, a imagem não foi executada localmente. O Dockerfile foi validado estaticamente pelo Hadolint, o `compose.yaml` foi validado contra o schema oficial e o workflow de CI contém uma etapa de build real da imagem em um runner com Docker.

## Azure

Nenhuma assinatura Azure foi usada nesta revisão. Não foram criados recursos, URL pública ou capturas de um ambiente remoto. O Bicep, os scripts e o workflow estão preparados para execução futura em uma assinatura válida.

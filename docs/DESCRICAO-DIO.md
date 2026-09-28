# Textos para publicação

## Nome sugerido do repositório

cloud-deployments-api

## Descrição curta para o GitHub

API REST em .NET 8 containerizada com Docker e preparada para deploy no Azure App Service via ACR, Bicep e GitHub Actions.

## Título para a DIO

Cloud Deployments API — Deploy de uma API .NET 8 no Azure

## Descrição para a entrega

Este projeto implementa uma API REST original em .NET 8 para registrar e acompanhar deployments de aplicações nos ambientes de desenvolvimento, staging e produção. A solução permite criar, consultar, filtrar, atualizar e excluir registros, além de disponibilizar endpoint de saúde e documentação Swagger/OpenAPI.

A aplicação foi preparada para execução em container Docker e implantação no Azure App Service. A arquitetura utiliza Azure Container Registry para armazenar imagens, identidade gerenciada com permissão AcrPull para evitar credenciais no Web App e infraestrutura declarada em Bicep. Também foi incluído um pipeline de integração contínua que valida formatação, compila, executa testes e verifica a construção da imagem.

O workflow de entrega contínua utiliza autenticação OpenID Connect, publica imagens com tags imutáveis baseadas no commit e atualiza o Azure Web App somente quando acionado manualmente. O repositório contém contrato OpenAPI, exemplos de requisições, documentação de segurança e instruções completas de criação e exclusão dos recursos Azure.

Na validação local, os 18 testes automatizados foram aprovados, com 100% de cobertura de linhas e branches, build sem avisos e nenhuma dependência vulnerável ou obsoleta identificada.

Como não foi utilizada uma assinatura Azure durante a elaboração, o repositório não apresenta URL ou capturas de um deploy remoto inexistente. Todo o fluxo está implementado e documentado para execução em uma assinatura válida.

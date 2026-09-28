# Evidências de um deploy real

Este arquivo é um checklist. Não contém capturas ou URLs simuladas.

Depois de executar o deploy em uma assinatura Azure, adicione evidências reais dos seguintes itens:

- [ ] execução bem-sucedida do workflow de CI;
- [ ] imagem e tag existentes no Azure Container Registry;
- [ ] Azure Web App em execução;
- [ ] identidade gerenciada habilitada no Web App;
- [ ] atribuição `AcrPull` no ACR;
- [ ] resposta de `GET /healthz` no ambiente Azure;
- [ ] página do Swagger carregada pelo endereço público;
- [ ] criação e consulta de um deployment pela API publicada;
- [ ] workflow manual de deploy concluído, se o OIDC tiver sido configurado;
- [ ] exclusão do grupo de recursos quando o ambiente não for mais necessário.

## Cuidados

- oculte IDs de assinatura, tenant e principal quando não forem necessários;
- nunca mostre tokens, passwords, publish profiles ou credenciais do ACR;
- não informe que houve deploy se ele não foi realmente executado;
- verifique os custos antes de manter os recursos ativos.

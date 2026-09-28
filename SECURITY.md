# Política de segurança

## Escopo

Este é um projeto educacional. Ele demonstra práticas seguras de build e deploy, mas não substitui uma avaliação completa para produção.

## Medidas adotadas

- validação das entradas HTTP;
- respostas de erro padronizadas;
- rate limiting básico;
- container executado como usuário sem privilégios;
- sistema de arquivos somente leitura no Docker Compose;
- ACR sem usuário administrador;
- identidade gerenciada para o pull da imagem;
- OIDC para o GitHub Actions;
- HTTPS, TLS mínimo 1.2 e FTPS desabilitado no template Azure;
- nenhum segredo versionado no repositório.

## Recomendações antes de produção

- adicionar Microsoft Entra ID ou outro mecanismo de autenticação;
- substituir o repositório em memória por um banco gerenciado;
- aplicar autorização por função;
- habilitar Application Insights e alertas;
- restringir redes e origens conforme a arquitetura;
- revisar dependências e imagens continuamente;
- proteger o ambiente `production` do GitHub com aprovação manual;
- aplicar princípio do menor privilégio às identidades Azure.

## Relato de vulnerabilidades

Não publique credenciais ou dados sensíveis em uma issue. Em um repositório real, use um canal privado definido pelo proprietário para comunicar a vulnerabilidade.

# Decisões de arquitetura (ADR)

Cada arquivo aqui registra uma decisão relevante: o contexto, o que foi decidido,
as alternativas consideradas e as consequências.

O objetivo é evitar que uma escolha vire "sempre foi assim". Quem entrar depois
consegue ver o que já foi avaliado e descartado — e, principalmente, **em que
condições a decisão deve ser revista**.

| # | Decisão | Situação |
|---|---|---|
| [0001](0001-exclusao-de-enderecos.md) | Exclusão de endereços: permanente ou lógica? | Aceita |
| [0002](0002-tabela-local-de-usuarios.md) | Manter uma tabela local de usuários espelhando o Keycloak | Aceita |

## Formato

Registros curtos, em português, seguindo a estrutura:

- **Contexto** — qual problema motivou a decisão
- **Decisão** — o que foi escolhido
- **Alternativas** — o que foi considerado e por que não foi adotado
- **Quando revisar** — o gatilho que torna a decisão obsoleta
- **Consequências** — o que muda no dia a dia por causa dela

Decisões pendentes de registro estão mapeadas na
[issue #27](https://github.com/LeonardoVieiraGuimaraes/gerenciamento-endereco/issues/27).

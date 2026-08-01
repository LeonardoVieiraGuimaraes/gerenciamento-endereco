# ADR 0002 — Manter uma tabela local de usuários espelhando o Keycloak

**Data:** 2026-08-01
**Situação:** aceita
**Decisão:** manter a tabela `Usuarios` no banco da aplicação, como **espelho**
do Keycloak — sem credenciais e sem ser fonte da verdade

---

## Contexto

Quem controla a identidade é o Keycloak: conta, senha, e-mail, perfis e 2FA
vivem lá. Ainda assim, a aplicação mantém uma tabela `Usuarios` própria.

Isso levanta uma dúvida legítima: se o Keycloak já tem os usuários, por que
duplicar? Duplicar dado normalmente é um problema — cria duas versões da verdade
que podem divergir.

## Alternativas consideradas

### A) Não ter tabela local — guardar o `sub` direto em `Enderecos`

`Enderecos.KeycloakUserId` (texto), sem tabela de usuários.

Rejeitada:
- Toda listagem que mostra o nome do dono precisaria consultar a API do Keycloak.
  Na tela do admin, com N endereços, seriam N chamadas de rede — o clássico
  problema N+1, agravado por sair da máquina.
- **Filtrar e ordenar por nome deixaria de funcionar em SQL.** O filtro por nome
  na tela de endereços seria impossível sem trazer todos os registros para a
  memória.
- Sem chave estrangeira, o banco perde a garantia de integridade: nada impediria
  um endereço apontar para usuário inexistente.

### B) Tabela local mínima — só `Id` + `KeycloakId`, sem dados pessoais

Resolve integridade e chave estrangeira, mas mantém o problema de exibição:
nome e login continuariam vindo da API a cada listagem.

Rejeitada pelo mesmo motivo de desempenho da alternativa A.

### C) Espelho com os dados de exibição (escolhida)

A tabela guarda `Id`, `KeycloakId`, `Username` e `Nome`.

## Decisão

Manter a tabela como espelho, respeitando três limites:

1. **Nunca é fonte da verdade.** Criar, editar ou excluir usuário acontece no
   Keycloak (via API administrativa). O espelho apenas acompanha.
2. **Não guarda credencial.** A coluna `Senha` existe porque o enunciado do teste
   pedia essa estrutura, mas nunca recebe senha — fica com um valor fixo
   indicando que a autenticação é externa.
3. **Só guarda o necessário para exibir e relacionar.** Nome e nome de usuário,
   nada além disso. E-mail, telefone e perfis continuam apenas no Keycloak.

O vínculo é feito pelo identificador imutável do Keycloak (`sub`), não pelo nome
de usuário — ver [ADR 0001](0001-exclusao-de-enderecos.md) e a issue #33 para o
problema que isso resolveu.

## Consequências

- **A cópia precisa ser mantida em dia.** Nome e nome de usuário são
  ressincronizados a cada acesso da pessoa. Sem isso, uma alteração no Keycloak
  ficaria invisível na aplicação para sempre.
- **Exclusão precisa ser em cascata.** Apagar a conta no Keycloak sem apagar o
  espelho deixaria dado pessoal órfão — tratado no fluxo de exclusão.
- Consultas de listagem e filtro continuam sendo resolvidas em SQL, sem chamada
  de rede.
- A integridade entre endereço e dono fica garantida pelo banco.

## Quando revisar

- **Se passarem a existir muitos atributos do usuário na aplicação.** Espelhar
  cada vez mais campos é sinal de que a fronteira está errada — nesse caso, vale
  buscar os dados sob demanda e usar cache, em vez de aumentar o espelho.
- **Se a sincronização por acesso não for suficiente.** Hoje a cópia só atualiza
  quando a pessoa entra. Se for necessário refletir alterações imediatamente, o
  caminho é escutar eventos do Keycloak (*admin events*) em vez de sincronizar
  na leitura.

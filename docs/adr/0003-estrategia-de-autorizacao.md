# ADR 0003 — Estratégia de autorização e caminho de evolução

**Data:** 2026-08-01
**Situação:** aceita
**Decisão:** Keycloak para autenticação e autorização por papel; a checagem de
dono do registro fica na aplicação. Em cenário complexo, um autorizador dedicado
(OpenFGA) entra como terceira camada — sem substituir o Keycloak

---

## Contexto

Autorização não é uma coisa só. Existem perguntas de granularidade diferente, e
cada uma tem o lugar certo para ser respondida:

| Pergunta | Granularidade |
|---|---|
| Quem é esta pessoa? | Autenticação |
| Que tipo de ação ela pode fazer? | Autorização por papel (*coarse-grained*) |
| Ela pode fazer isso **neste registro**? | Autorização por dado (*fine-grained*) |

Tratar as três no mesmo lugar é o erro comum — leva ou a colocar regra de
negócio dentro do servidor de identidade, ou a reimplementar papéis dentro da
aplicação.

## Decisão

### Hoje — duas camadas (ABAC híbrido)

**Keycloak: autenticação + papéis apenas**

Emite o token com a identidade e **apenas dois papéis**: `ADMIN` e `USUARIO`.
Nada mais — sem micro-permissões dentro do Keycloak. A separação de permissões
fica toda na aplicação.

**Aplicação: ABAC + row-level authorization**

A aplicação implementa atributos de dois tipos:

1. **Permissões por ação** (coarse-grained): Baseado na role que veio do Keycloak,
   converte em políticas do ASP.NET Core, separadas por ação — ler, escrever,
   excluir, exportar, gerenciar usuários. Fica em `Program.cs`:
   ```csharp
   // Se tem role ADMIN, libera tudo. Se tem USUARIO, libera o básico.
   options.AddPolicy("EnderecoWrite", policy => policy.RequireAssertion(context =>
       context.User.IsInRole("ADMIN") || context.User.IsInRole("USUARIO")
   ));
   ```

2. **Permissões por dado** (fine-grained, row-level): Verifica se o usuário é
   dono do registro. Implementado nos controllers:
   ```csharp
   // ADMIN enxerga tudo. USUARIO enxerga só os seus.
   if (usuarioEhAdmin) 
       return todoEnderecos;
   else
       return enderecosDoUser;
   ```

Esse é o padrão ABAC porque a decisão leva em conta **atributos do usuário**
(role) **e do recurso** (proprietário), não apenas a role.

### Amanhã — terceira camada, se necessário

Em sistema grande, com regras que não cabem numa consulta, entra um **autorizador
dedicado**. A referência atual de mercado é o **OpenFGA** (projeto CNCF, baseado
no modelo Zanzibar do Google); alternativas equivalentes são Cerbos, SpiceDB e
Open Policy Agent.

O arranjo seria:

```
Keycloak   →  quem é a pessoa + papéis e grupos
    ↓
OpenFGA    →  esta pessoa pode fazer X neste recurso Y?
    ↓
Aplicação  →  executa
```

Repare que **o Keycloak continua**. O autorizador não o substitui: um cuida da
identidade, o outro das relações entre pessoas e recursos.

### Por que não guardar permissões no Keycloak?

Três motivos principais:

1. **Acoplamento**: Cada permissão nova exigiria alterar o Keycloak e redescobrir
   o token em cliente. A aplicação é o lado que muda — mantê-la independente é
   mais ágil.

2. **Responsabilidades**: O Keycloak cuida de "quem você é"; a aplicação cuida
   de "o que você pode fazer com o que você vê". Misturar dificulta evolução.

3. **Escala**: Permissões por dados (endereço X pertence a usuário Y?) exigem
   consultar o banco de forma complexa em tempo de token, ou duplicar dados no
   Keycloak. Mais fácil resolver junto da query que acessa os dados.

## Alternativas consideradas

### Keycloak Authorization Services para tudo

O Keycloak tem recurso próprio de autorização por recurso (UMA 2.0). Rejeitado
como caminho de evolução:

- Exige **registrar cada recurso** dentro do Keycloak. Cada endereço novo viraria
  uma chamada de API para criá-lo lá, acoplando o cadastro do dado ao servidor de
  identidade.
- Não foi projetado para volume alto de recursos com relações entre si.
- Na prática, mesmo quem externaliza autorização tende a não usar esse recurso —
  ferramentas como Cerbos se integram ao Keycloak justamente para expressar o que
  ele não expressa bem.

### Autorizador dedicado desde já

Rejeitado pelo estágio atual. A regra é "o dono vê o dele; ADMIN vê tudo" — cabe
numa cláusula `WHERE`, resolvida junto de uma consulta que aconteceria de
qualquer forma. Um serviço externo acrescentaria um container, uma chamada de
rede por requisição e sincronização de dados, sem ganho.

## Quando adotar o autorizador dedicado

Gatilhos concretos — qualquer um deles justifica:

- [ ] **Compartilhamento entre usuários** — "Maria compartilhou este endereço com
      João, que pode ver mas não editar"
- [ ] **Hierarquia** — "o gerente enxerga os endereços de toda a equipe dele"
- [ ] **Multi-tenant** — permissões variando por organização
- [ ] **Regras ajustadas por quem não programa**, num painel, sem alterar código
- [ ] **Vários serviços** precisando da mesma decisão, sem duplicar a lógica

O sinal de alerta prático: quando a cláusula `WHERE` da consulta começar a
carregar regra de permissão complexa, ou quando a mesma regra aparecer copiada em
mais de um serviço.

## Consequências

- A aplicação não depende de infraestrutura extra para autorizar
- Papéis são administrados no Keycloak, sem alterar código
- A migração futura é viável sem reescrita: as políticas estão centralizadas em
  um único ponto (`Program.cs`) e a checagem de dono está isolada em serviço, não
  espalhada pelas telas
- Enquanto isso, o custo é ter a regra de dono expressa em consulta — o que exige
  disciplina para nunca buscar o registro primeiro e conferir o dono depois

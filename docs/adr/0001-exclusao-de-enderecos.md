# ADR 0001 — Exclusão de endereços: permanente ou lógica?

**Data:** 2026-08-01
**Situação:** aceita
**Decisão:** manter a **exclusão permanente** (o registro sai do banco)

---

## Contexto

A tela e a API de endereços hoje apagam o registro de verdade (`DELETE` no banco).
A alternativa comum é a **exclusão lógica** (*soft delete*): manter a linha e
marcar como inativa, filtrando as inativas nas consultas.

A pergunta é qual das duas é a prática correta neste sistema.

## O que pesa na decisão

O ponto que define a resposta **não é preferência, é quem referencia o dado**.

Hoje, no modelo do sistema, **nada aponta para um endereço** além do próprio dono:

```
Usuario 1 ──── N Endereco
```

Não existem pedidos, entregas, notas ou contratos ligados a um endereço. Ele é um
dado que pertence ao usuário e é consumido apenas por ele.

### Argumentos a favor da exclusão permanente (escolhida)

1. **LGPD.** Endereço residencial é dado pessoal. O direito de eliminação
   (Art. 18, VI) espera que o dado realmente saia. Exclusão lógica mantém o dado
   pessoal no banco — só escondido da tela.
2. **Não há histórico a preservar.** Sem nada referenciando o endereço, apagar
   não corrompe nem invalida outro registro.
3. **Simplicidade.** Exclusão lógica obriga a filtrar "não excluído" em toda
   consulta. Esquecer o filtro em um lugar faz dado apagado reaparecer — é um
   erro silencioso e comum.
4. **Índices e unicidade.** Registros inativos continuam ocupando espaço e
   atrapalham restrições de unicidade.

### Argumentos a favor da exclusão lógica (rejeitados neste momento)

1. *Recuperar exclusão acidental* — resolvido melhor por confirmação na tela (já
   existe) e por backup (issue #18).
2. *Manter histórico* — resolvido pela trilha de auditoria (issue #11), que
   registra o que foi excluído sem manter a linha ativa.

## Quando esta decisão deve ser revista

**Reveja assim que qualquer outro registro passar a referenciar um endereço** —
por exemplo, pedido, entrega, cobrança ou contrato.

Nesse cenário, apagar o endereço quebraria o histórico: um pedido entregue no
endereço X precisa continuar mostrando X, mesmo que a pessoa tenha apagado ou
alterado esse endereço depois.

Mas atenção: **a solução ali também não é exclusão lógica**, e sim **cópia no
momento do uso** (*snapshot*). O pedido guarda os campos do endereço como estavam
na data da entrega. Assim:

- O histórico fica correto e imutável
- O usuário continua livre para editar ou apagar os próprios endereços
- Não é preciso filtrar "inativo" em consulta nenhuma

Exclusão lógica só passa a valer a pena se houver requisito explícito de
"desativar temporariamente e reativar depois" — que não é o caso.

## Alternativa intermediária, se houver demanda

Se surgir necessidade de desfazer exclusão acidental sem depender de backup,
o padrão adequado é uma **lixeira com prazo**: o registro é marcado como excluído,
some da interface imediatamente e é **apagado de vez automaticamente** após um
prazo (por exemplo, 30 dias).

Isso concilia recuperação e LGPD — diferente da exclusão lógica pura, o dado
realmente deixa de existir, só que com um intervalo de arrependimento.

## Consequências

- A exclusão continua como está: definitiva, com confirmação na tela
- A recuperação de erro depende de backup (issue #18) — o que reforça a
  prioridade dela
- Quando a trilha de auditoria (issue #11) existir, ela deve registrar a
  exclusão com os dados anteriores, para responder "o que foi apagado e por quem"
- Se o sistema ganhar pedidos ou entregas, aplicar *snapshot* — e não converter
  o endereço para exclusão lógica

using GerenciamentoEndereco.API.Models;
using Microsoft.EntityFrameworkCore;

namespace GerenciamentoEndereco.API.Data;

/// <summary>
/// Popula o banco com usuários e endereços para homologação e demonstração.
///
/// Roda apenas quando a configuração <c>Seed:Enabled</c> está ligada — em
/// produção de verdade deve ficar desligada, para não inserir dado fictício
/// junto de dado real.
///
/// É idempotente: se os registros já existem, não faz nada. Isso importa porque
/// o método roda a cada boot do container, e um deploy não pode duplicar dados.
///
/// Os identificadores usados aqui são os mesmos definidos no realm do Keycloak
/// (auth-keycloak/realm/gerenciamento-endereco-realm.json). Fixá-los dos dois
/// lados é o que permite criar o registro local já vinculado à identidade certa,
/// antes mesmo do primeiro login da pessoa.
/// </summary>
public static class DadosDeTeste
{
    private record UsuarioSemente(string KeycloakId, string Username, string Nome);

    private static readonly UsuarioSemente[] Usuarios =
    [
        new("00000000-0000-4000-a000-000000000002", "leonardo", "Leonardo Guimaraes"),
        new("00000000-0000-4000-a000-000000000003", "maria",    "Maria Oliveira"),
        new("00000000-0000-4000-a000-000000000004", "joao",     "Joao Pereira"),
        new("00000000-0000-4000-a000-000000000005", "ana",      "Ana Souza"),
        new("00000000-0000-4000-a000-000000000006", "carlos",   "Carlos Santos"),
    ];

    /// <summary>Endereços por usuário, com CEPs reais de capitais diferentes.</summary>
    private static readonly Dictionary<string, (string Cep, string Logradouro, string Numero, string Bairro, string Cidade, string Uf)[]> Enderecos = new()
    {
        ["leonardo"] =
        [
            ("30130-110", "Avenida Afonso Pena",        "1500", "Centro",           "Belo Horizonte", "MG"),
            ("32280-580", "Rua Rio Hudson",             "648",  "Riacho das Pedras", "Contagem",      "MG"),
        ],
        ["maria"] =
        [
            ("01310-100", "Avenida Paulista",           "1578", "Bela Vista",       "São Paulo",      "SP"),
            ("20040-020", "Rua da Assembleia",          "77",   "Centro",           "Rio de Janeiro", "RJ"),
            ("69020-160", "Avenida Eduardo Ribeiro",    "520",  "Centro",           "Manaus",         "AM"),
        ],
        ["joao"] =
        [
            ("80010-000", "Rua XV de Novembro",         "400",  "Centro",           "Curitiba",       "PR"),
            ("90020-000", "Avenida Borges de Medeiros", "1200", "Centro Histórico", "Porto Alegre",   "RS"),
            ("88010-400", "Rua Felipe Schmidt",         "300",  "Centro",           "Florianópolis",  "SC"),
        ],
        ["ana"] =
        [
            ("40020-000", "Avenida Sete de Setembro",   "3003", "Centro",                   "Salvador",  "BA"),
            ("50030-230", "Rua da Aurora",              "911",  "Boa Vista",                "Recife",    "PE"),
            ("60160-230", "Avenida Beira Mar",          "3130", "Meireles",                 "Fortaleza", "CE"),
            ("70040-010", "Esplanada dos Ministérios",  "1",    "Zona Cívico-Administrativa", "Brasília", "DF"),
        ],
        ["carlos"] =
        [
            ("29050-275", "Avenida Nossa Senhora dos Navegantes", "675", "Enseada do Suá", "Vitória", "ES"),
            ("74023-010", "Avenida Goiás",              "600",  "Setor Central",    "Goiânia",        "GO"),
        ],
    };

    public static async Task AplicarAsync(AppDbContext context, ILogger logger)
    {
        // Só semeia num banco vazio. Se já houver endereço, presume-se que o
        // ambiente está em uso e nada deve ser tocado.
        if (await context.Enderecos.AnyAsync())
        {
            logger.LogInformation("Dados de teste ignorados: já existem endereços no banco.");
            return;
        }

        var criados = 0;

        foreach (var semente in Usuarios)
        {
            var usuario = await context.Usuarios
                .FirstOrDefaultAsync(u => u.KeycloakId == semente.KeycloakId
                                       || u.Username == semente.Username);

            if (usuario == null)
            {
                usuario = new Usuario
                {
                    KeycloakId = semente.KeycloakId,
                    Username = semente.Username,
                    Nome = semente.Nome,
                    Senha = "KEYCLOAK_MANAGED"
                };
                context.Usuarios.Add(usuario);
                await context.SaveChangesAsync();
            }
            else if (string.IsNullOrEmpty(usuario.KeycloakId))
            {
                // Registro criado por acesso anterior, antes de existir o vínculo.
                usuario.KeycloakId = semente.KeycloakId;
                await context.SaveChangesAsync();
            }

            if (!Enderecos.TryGetValue(semente.Username, out var lista))
                continue;

            foreach (var e in lista)
            {
                context.Enderecos.Add(new Endereco
                {
                    Cep = e.Cep,
                    Logradouro = e.Logradouro,
                    Numero = e.Numero,
                    Bairro = e.Bairro,
                    Cidade = e.Cidade,
                    Uf = e.Uf,
                    UsuarioId = usuario.Id
                });
                criados++;
            }
        }

        await context.SaveChangesAsync();
        logger.LogInformation("Dados de teste aplicados: {Usuarios} usuários e {Enderecos} endereços.",
            Usuarios.Length, criados);
    }
}

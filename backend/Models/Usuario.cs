using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GerenciamentoEndereco.API.Models;

[Table("Usuarios")]
public class Usuario
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "O nome é obrigatório.")]
    [StringLength(100)]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "O usuário de login é obrigatório.")]
    [StringLength(50)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Identificador do usuário no Keycloak (claim "sub").
    ///
    /// É o vínculo confiável entre a identidade e este registro local. Diferente
    /// do nome de usuário, o "sub" é imutável e nunca reaproveitado: se a conta
    /// for excluída e outra for criada com o mesmo nome, o "sub" será outro — e
    /// a conta nova não herda os endereços da antiga.
    ///
    /// Aceita nulo apenas por causa dos registros criados antes desta coluna
    /// existir; eles são preenchidos no primeiro acesso de cada usuário.
    /// </summary>
    [StringLength(50)]
    public string? KeycloakId { get; set; }

    [Required(ErrorMessage = "A senha é obrigatória.")]
    [StringLength(200)]
    public string Senha { get; set; } = string.Empty;

    public ICollection<Endereco> Enderecos { get; set; } = new List<Endereco>();
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GerenciamentoEndereco.API.Models;

[Table("Enderecos")]
public class Endereco
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "O CEP é obrigatório.")]
    [StringLength(10)]
    public string Cep { get; set; } = string.Empty;

    [Required(ErrorMessage = "O Logradouro é obrigatório.")]
    [StringLength(200)]
    public string Logradouro { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Complemento { get; set; }

    [Required(ErrorMessage = "O Bairro é obrigatório.")]
    [StringLength(100)]
    public string Bairro { get; set; } = string.Empty;

    [Required(ErrorMessage = "A Cidade é obrigatória.")]
    [StringLength(100)]
    public string Cidade { get; set; } = string.Empty;

    [Required(ErrorMessage = "A UF é obrigatória.")]
    [StringLength(2)]
    public string Uf { get; set; } = string.Empty;

    [Required(ErrorMessage = "O Número é obrigatório.")]
    [StringLength(20)]
    public string Numero { get; set; } = string.Empty;

    [Required]
    public int UsuarioId { get; set; }

    [ForeignKey("UsuarioId")]
    public Usuario? Usuario { get; set; }
}

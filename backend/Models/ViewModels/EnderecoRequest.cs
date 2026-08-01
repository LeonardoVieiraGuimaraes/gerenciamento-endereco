using System.ComponentModel.DataAnnotations;

namespace GerenciamentoEndereco.API.Models.ViewModels;

/// <summary>
/// Dados aceitos ao criar ou atualizar um endereço pela API.
///
/// Deliberadamente não expõe Id nem UsuarioId: o dono do endereço é sempre
/// deduzido de quem está autenticado. Aceitar UsuarioId do cliente permitiria
/// gravar endereço na conta de outra pessoa.
/// </summary>
public class EnderecoRequest
{
    [Required(ErrorMessage = "O CEP é obrigatório.")]
    [StringLength(10, ErrorMessage = "O CEP deve ter no máximo 10 caracteres.")]
    public string Cep { get; set; } = string.Empty;

    [Required(ErrorMessage = "O logradouro é obrigatório.")]
    [StringLength(200, ErrorMessage = "O logradouro deve ter no máximo 200 caracteres.")]
    public string Logradouro { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "O complemento deve ter no máximo 200 caracteres.")]
    public string? Complemento { get; set; }

    [Required(ErrorMessage = "O bairro é obrigatório.")]
    [StringLength(100, ErrorMessage = "O bairro deve ter no máximo 100 caracteres.")]
    public string Bairro { get; set; } = string.Empty;

    [Required(ErrorMessage = "A cidade é obrigatória.")]
    [StringLength(100, ErrorMessage = "A cidade deve ter no máximo 100 caracteres.")]
    public string Cidade { get; set; } = string.Empty;

    [Required(ErrorMessage = "A UF é obrigatória.")]
    [StringLength(2, MinimumLength = 2, ErrorMessage = "A UF deve ter exatamente 2 caracteres.")]
    public string Uf { get; set; } = string.Empty;

    [Required(ErrorMessage = "O número é obrigatório.")]
    [StringLength(20, ErrorMessage = "O número deve ter no máximo 20 caracteres.")]
    public string Numero { get; set; } = string.Empty;
}

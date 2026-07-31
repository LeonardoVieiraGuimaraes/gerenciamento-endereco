using System.ComponentModel.DataAnnotations;

namespace GerenciamentoEndereco.API.Models.ViewModels
{
    public class EditUsuarioViewModel
    {
        public int Pk { get; set; }

        public string? KeycloakId { get; set; }

        public string? Username { get; set; }

        [Required(ErrorMessage = "O nome é obrigatório.")]
        public string? Name { get; set; }

        [Required(ErrorMessage = "O e-mail é obrigatório.")]
        [EmailAddress(ErrorMessage = "E-mail inválido.")]
        public string? Email { get; set; }

        public bool IsActive { get; set; }

        [DataType(DataType.Password)]
        public string? NovaSenha { get; set; }
    }
}

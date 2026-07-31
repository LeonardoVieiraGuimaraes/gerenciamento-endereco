namespace GerenciamentoEndereco.API.Models.ViewModels
{
    public class PerfilViewModel
    {
        public string Nome { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new();
        public int EnderecoCount { get; set; }
    }
}

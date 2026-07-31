namespace GerenciamentoEndereco.API.Models.ViewModels
{
    public class UsuarioListViewModel
    {
        public string? Id { get; set; }
        public string? Nome { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public bool Enabled { get; set; }
        public int EnderecoCount { get; set; }
    }
}

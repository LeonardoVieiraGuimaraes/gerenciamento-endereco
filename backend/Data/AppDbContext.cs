using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using GerenciamentoEndereco.API.Models;

namespace GerenciamentoEndereco.API.Data;

public class AppDbContext : DbContext, IDataProtectionKeyContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<Endereco> Enderecos { get; set; }
    // Tabela gerenciada automaticamente pelo ASP.NET Data Protection
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuração de relacionamento de 1 para N
        modelBuilder.Entity<Endereco>()
            .HasOne(e => e.Usuario)
            .WithMany(u => u.Enderecos)
            .HasForeignKey(e => e.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade); // Se deletar o usuário, deleta os endereços
    }
}

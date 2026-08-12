using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using AgiVysSystem.Api.Data;

namespace AgiVysSystem.Api;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        // Usa versão hardcoded para evitar AutoDetect que tenta conectar ao banco
        optionsBuilder.UseMySql(
            "Server=localhost;Database=agivys_bd;Uid=root;Pwd=x;",
            new MySqlServerVersion(new Version(8, 0, 0))
        );
        return new AppDbContext(optionsBuilder.Options);
    }
}

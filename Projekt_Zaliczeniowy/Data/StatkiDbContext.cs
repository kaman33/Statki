using System.IO;
using Microsoft.EntityFrameworkCore;

namespace Projekt_Zaliczeniowy.Data;

public class StatkiDbContext : DbContext
{
    public DbSet<GameResult> GameResults => Set<GameResult>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        string databasePath = Path.Combine(AppContext.BaseDirectory, "statki.db");
        optionsBuilder.UseSqlite($"Data Source={databasePath}");
    }
}

using Microsoft.EntityFrameworkCore;

namespace Projekt_Zaliczeniowy.Data;

public class GameResultRepository
{
    public GameResultRepository()
    {
        using StatkiDbContext dbContext = new();
        dbContext.Database.EnsureCreated();
    }

    public List<GameResult> GetLatestResults()
    {
        using StatkiDbContext dbContext = new();

        return dbContext.GameResults
            .AsNoTracking()
            .OrderByDescending(result => result.PlayedAt)
            .Take(20)
            .ToList();
    }

    public void Add(GameResult result)
    {
        using StatkiDbContext dbContext = new();
        dbContext.GameResults.Add(result);
        dbContext.SaveChanges();
    }
}

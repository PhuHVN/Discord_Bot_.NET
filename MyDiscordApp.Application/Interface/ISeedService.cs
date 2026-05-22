public interface ISeedService
{
    Task<List<SeedDto>> GetSeedInfoAsync();
}
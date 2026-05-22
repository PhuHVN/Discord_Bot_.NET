public class SeedService : ISeedService
{
    public async Task<List<SeedDto>> GetSeedInfoAsync()
    {
        var rs = await Task.FromResult(new List<SeedDto>
        {
            new SeedDto
            {
                Seed = "Hạt dưa hấu 🍉",
                Quantity = 1
            },
            new SeedDto
            {
                Seed = "Hạt bí đỏ 🍅",
                Quantity = 1
            },
            new SeedDto
            {
                Seed = "Hạt hướng dương ☀️",
                Quantity = 2
            }
        });
        return rs;
    }
}
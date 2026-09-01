using BirdApp.Models;
using MongoDB.Driver;

namespace BirdApp.Database;

public class MongoDbService
{
    private readonly IMongoDatabase _database;
    private readonly IMongoCollection<Bird> _birds;

    private readonly IMongoCollection<Observation> _observations;

    public MongoDbService()
    {
        var client = new MongoClient("mongodb://localhost:27017");

        _database = client.GetDatabase("BirdApp");

        _birds = _database.GetCollection<Bird>("birds");

        _observations = _database.GetCollection<Observation>("observations");

        CreateIndexes();
    }

    private void CreateIndexes()
    {
        var indexKeys = Builders<Bird>.IndexKeys
            .Ascending(b => b.TaxonId);

        var indexOptions = new CreateIndexOptions
        {
            Unique = true
        };

        var indexModel = new CreateIndexModel<Bird>(
            indexKeys,
            indexOptions);

        _birds.Indexes.CreateOne(indexModel);
    }

    public async Task SaveBirdsAsync(List<Bird> birds)
    {
        foreach (var bird in birds)
        {
            var filter = Builders<Bird>.Filter
                .Eq(b => b.TaxonId, bird.TaxonId);

            await _birds.ReplaceOneAsync(
                filter,
                bird,
                new ReplaceOptions
                {
                    IsUpsert = true
                });
        }

        Console.WriteLine(
            $"Spremljeno/ažurirano ptica: {birds.Count}");
    }

    public async Task<List<Bird>> GetBirdsAsync()
    {
        return await _birds
            .Find(_ => true)
            .ToListAsync();
    }

    public async Task SaveObservationAsync(Observation observation)
    {
        await _observations.InsertOneAsync(observation);
    }
}
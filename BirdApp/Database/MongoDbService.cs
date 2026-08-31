using MongoDB.Driver;

namespace BirdApp.Database;

public class MongoDbService
{
    private readonly IMongoDatabase _database;

    public MongoDbService()
    {
        var client = new MongoClient("mongodb://localhost:27017");

        _database = client.GetDatabase("BirdApp");
    }

    public async Task TestConnectionAsync()
    {
        await _database.RunCommandAsync<MongoDB.Bson.BsonDocument>(
            new MongoDB.Bson.BsonDocument("ping", 1));

        Console.WriteLine("MongoDB veza uspješna!");
    }
}
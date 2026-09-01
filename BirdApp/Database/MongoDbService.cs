using BirdApp.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace BirdApp.Database;

public class MongoDbService
{
    private readonly IMongoDatabase _database;
    private readonly IMongoCollection<Bird> _birds;
    private readonly IMongoCollection<Observation> _observations;
    private readonly IMongoCollection<AudioFile> _audioFiles;
    private readonly IMongoCollection<AudioClassification> _audioClassifications;

    public MongoDbService()
    {
        var client = new MongoClient("mongodb://localhost:27017");

        _database = client.GetDatabase("BirdApp");

        _birds = _database.GetCollection<Bird>("birds");

        _audioFiles = _database.GetCollection<AudioFile>("audioFiles");

        _observations = _database.GetCollection<Observation>("observations");

        _audioClassifications =_database.GetCollection<AudioClassification>("audioClassifications");

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

        var classificationIndexKeys =
    Builders<AudioClassification>.IndexKeys
        .Ascending(c => c.AudioFileId)
        .Ascending(c => c.StartTime)
        .Ascending(c => c.EndTime)
        .Ascending(c => c.ScientificName);

        var classificationIndexOptions =
            new CreateIndexOptions
            {
                Unique = true
            };

        var classificationIndex =
            new CreateIndexModel<AudioClassification>(
                classificationIndexKeys,
                classificationIndexOptions);

        _audioClassifications.Indexes.CreateOne(
            classificationIndex);
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
        var bsonDocument = new BsonDocument
    {
        { "TaxonId", observation.TaxonId },
        { "Latitude", observation.Latitude },
        { "Longitude", observation.Longitude },
        {
            "BiologicalData",
            BsonDocument.Parse(
                JsonSerializer.Serialize(
                    observation.BiologicalData))
        }
    };

        await _database
            .GetCollection<BsonDocument>("observations")
            .InsertOneAsync(bsonDocument);
    }

    public async Task SaveAudioFileAsync(AudioFile audioFile)
    {
        await _audioFiles.InsertOneAsync(audioFile);

        Console.WriteLine(
            $"Metadata spremljena u MongoDB: {audioFile.FileName}");
    }

    public async Task SaveAudioClassificationAsync(
    AudioClassification classification)
    {
        var filter =
            Builders<AudioClassification>.Filter.And(
                Builders<AudioClassification>.Filter.Eq(
                    c => c.AudioFileId,
                    classification.AudioFileId),

                Builders<AudioClassification>.Filter.Eq(
                    c => c.StartTime,
                    classification.StartTime),

                Builders<AudioClassification>.Filter.Eq(
                    c => c.EndTime,
                    classification.EndTime),

                Builders<AudioClassification>.Filter.Eq(
                    c => c.ScientificName,
                    classification.ScientificName)
            );

        var existing =
            await _audioClassifications
                .Find(filter)
                .FirstOrDefaultAsync();

        if (existing != null)
        {
            Console.WriteLine(
                $"Klasifikacija već postoji: " +
                $"{classification.FileName} - " +
                $"{classification.ScientificName}");

            return;
        }

        await _audioClassifications.InsertOneAsync(
            classification);

        Console.WriteLine(
            $"Klasifikacija spremljena: " +
            $"{classification.FileName} - " +
            $"{classification.ScientificName}");
    }

    public async Task<AudioFile?> GetAudioFileAsync(string fileName)
    {
        return await _audioFiles
            .Find(a => a.FileName == fileName)
            .FirstOrDefaultAsync();
    }

    public async Task<List<AudioFile>> GetAudioFilesAsync()
    {
        return await _audioFiles
            .Find(_ => true)
            .ToListAsync();
    }

    public async Task<Bird?> GetBirdByCanonicalNameAsync(
     string canonicalName)
    {
        var normalizedName = canonicalName.Trim();

        var birds = await _birds
            .Find(_ => true)
            .ToListAsync();

        return birds.FirstOrDefault(b =>
            string.Equals(
                b.CanonicalName?.Trim(),
                normalizedName,
                StringComparison.OrdinalIgnoreCase));
    }

    public async Task<List<ObjectId>> GetObservationIdsByTaxonIdAsync(
    long taxonId)
    {
        var observations =
            _database.GetCollection<BsonDocument>("observations");

        var filter =
            Builders<BsonDocument>.Filter.Eq(
                "TaxonId",
                taxonId);

        var documents = await observations
            .Find(filter)
            .ToListAsync();

        return documents
            .Where(document => document.Contains("_id"))
            .Select(document => document["_id"].AsObjectId)
            .ToList();
    }
}
using BirdApp.Database;
using BirdApp.Models;
using Confluent.Kafka;
using System.Text.Json;

namespace BirdApp.Kafka;

public class ObservationGenerator
{
    private readonly MongoDbService _database;

    public ObservationGenerator(MongoDbService database)
    {
        _database = database;
    }

    public async Task GenerateAsync(int numberOfObservations = 30)
    {
        Console.WriteLine("Provjeravam Kafka topic...");

        if (KafkaHasMessages())
        {
            Console.WriteLine(
                "Kafka već sadrži opažanja. Generator se preskače.");

            return;
        }

        Console.WriteLine(
            "Kafka je prazna. Generiram opažanja...");

        var birds = await _database.GetBirdsAsync();

        if (birds.Count == 0)
        {
            Console.WriteLine(
                "MongoDB ne sadrži niti jednu pticu.");

            return;
        }

        var selectedBirds = birds
            .OrderBy(_ => Random.Shared.Next())
            .Take(Math.Min(numberOfObservations, birds.Count))
            .ToList();

        var config = new ProducerConfig
        {
            BootstrapServers = "localhost:9092"
        };

        using var producer =
            new ProducerBuilder<Null, string>(config).Build();

        foreach (var bird in selectedBirds)
        {
            var observation = new Observation
            {
                TaxonId = bird.TaxonId,
                Latitude = Random.Shared.NextDouble() * 180 - 90,
                Longitude = Random.Shared.NextDouble() * 360 - 180,

                BiologicalData = new Dictionary<string, JsonElement>
                {
                    ["habitat"] =
                        JsonSerializer.SerializeToElement(
                            GetRandomHabitat()),

                    ["migrationStatus"] =
                        JsonSerializer.SerializeToElement(
                            GetRandomMigrationStatus())
                }
            };

            var json = JsonSerializer.Serialize(observation);

            await producer.ProduceAsync(
                "bird-observations",
                new Message<Null, string>
                {
                    Value = json
                });

            Console.WriteLine(
                $"Poslano opažanje: TaxonId={observation.TaxonId}");
        }

        producer.Flush(TimeSpan.FromSeconds(10));

        Console.WriteLine(
            $"Ukupno poslano opažanja: {selectedBirds.Count}");
    }

    private bool KafkaHasMessages()
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = "localhost:9092",
            GroupId = $"observation-check-{Guid.NewGuid()}",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer =
            new ConsumerBuilder<Ignore, string>(config).Build();

        consumer.Subscribe("bird-observations");

        var result = consumer.Consume(
            TimeSpan.FromSeconds(2));

        consumer.Close();

        return result != null;
    }

    private static string GetRandomHabitat()
    {
        string[] habitats =
        {
            "forest",
            "wetland",
            "grassland",
            "mountains",
            "coast"
        };

        return habitats[
            Random.Shared.Next(habitats.Length)];
    }

    private static string GetRandomMigrationStatus()
    {
        string[] statuses =
        {
            "resident",
            "migratory",
            "partially_migratory"
        };

        return statuses[
            Random.Shared.Next(statuses.Length)];
    }
}
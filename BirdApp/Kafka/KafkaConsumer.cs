using BirdApp.Models;
using Confluent.Kafka;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using BirdApp.Database;

namespace BirdApp.Kafka;
public class KafkaConsumer
{
    private readonly ConsumerConfig _config;

    private readonly MongoDbService _database;

    public KafkaConsumer(MongoDbService database)
    {
        _database = database;

        _config = new ConsumerConfig
        {
            BootstrapServers = "localhost:9092",
            GroupId = "birdapp-consumer-v4",
            AutoOffsetReset = AutoOffsetReset.Earliest
        };
    }

    public async Task<List<Observation>> ConsumeAsync()
    {
        var observations = new List<Observation>();

        using var consumer = new ConsumerBuilder<Ignore, string>(_config).Build();

        consumer.Subscribe("bird-observations");

        Console.WriteLine("Čekam Kafka poruke...");

        while (true)
        {
            var result = consumer.Consume(TimeSpan.FromSeconds(2));

            if (result == null)
                break;

            try
            {
                var observation = JsonSerializer.Deserialize<Observation>(
                    result.Message.Value);

                if (observation != null)
                {
                    observations.Add(observation);

                    Console.WriteLine(
                        $"BiologicalData count: {observation.BiologicalData.Count}");

                    await _database.SaveObservationAsync(observation);

                    Console.WriteLine(
                        $"Primljeno opažanje: TaxonId={observation.TaxonId}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greška kod čitanja poruke: {ex.Message}");
            }
        }

        consumer.Close();

        return observations;
    }
}

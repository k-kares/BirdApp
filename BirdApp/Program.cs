using BirdApp.Database;
using BirdApp.Kafka;
using BirdApp.Scraping;

var scraper = new BirdScraper();

var birds = await scraper.ScrapeAsync(1);

Console.WriteLine();
Console.WriteLine($"Ukupno obrađeno ptica: {birds.Count}");

var database = new MongoDbService();

await database.SaveBirdsAsync(birds);

var observationGenerator = new ObservationGenerator(database);

await observationGenerator.GenerateAsync(30);

var kafkaConsumer = new KafkaConsumer(database);

await kafkaConsumer.ConsumeAsync();

Console.WriteLine();
Console.WriteLine("Pritisni Enter za zatvaranje...");
Console.ReadLine();
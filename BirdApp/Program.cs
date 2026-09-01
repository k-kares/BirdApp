using BirdApp.Database;
using BirdApp.Kafka;
using BirdApp.Minio;
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

var minio = new MinioService(database);

string audioFolder = "Audio";

if (!Directory.Exists(audioFolder))
{
    Console.WriteLine($"Audio folder ne postoji: {audioFolder}");
    return;
}

var audioFiles = Directory.GetFiles(audioFolder)
    .Where(file =>
        file.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) ||
        file.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
    .ToList();

Console.WriteLine($"Pronađeno audio datoteka: {audioFiles.Count}");

foreach (var file in audioFiles)
{
    await minio.UploadAudioAsync(
        file,
        45.815,
        15.982);
}

Console.WriteLine("Sve audio datoteke su obrađene.");

Console.WriteLine();
Console.WriteLine("Pritisni Enter za zatvaranje...");
Console.ReadLine();
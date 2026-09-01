using BirdApp.Classification;
using BirdApp.Database;
using BirdApp.Kafka;
using BirdApp.Minio;
using BirdApp.Models;
using BirdApp.Scraping;

var database = new MongoDbService();

var existingBirds = await database.GetBirdsAsync();

List<Bird> birds;

if (existingBirds.Count > 0)
{
    Console.WriteLine(
        $"MongoDB već sadrži podatke o pticama: {existingBirds.Count}");

    Console.WriteLine(
        "Web scraping se preskače.");

    birds = existingBirds;
}
else
{
    Console.WriteLine(
        "MongoDB nema podatke o pticama.");

    Console.WriteLine(
        "Pokrećem web scraping...");

    var scraper = new BirdScraper();

    birds = await scraper.ScrapeAsync(50);

    Console.WriteLine();
    Console.WriteLine(
        $"Ukupno obrađeno ptica: {birds.Count}");

    await database.SaveBirdsAsync(birds);
}

var observationGenerator =
    new ObservationGenerator(database);

await observationGenerator.GenerateAsync(30);

var kafkaConsumer =
    new KafkaConsumer(database);

await kafkaConsumer.ConsumeAsync();

var minio =
    new MinioService(database);

string audioFolder = "Audio";

if (!Directory.Exists(audioFolder))
{
    Console.WriteLine(
        $"Audio folder ne postoji: {audioFolder}");
    return;
}

var audioFiles = Directory.GetFiles(audioFolder)
    .Where(file =>
        file.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) ||
        file.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
    .ToList();

Console.WriteLine(
    $"Pronađeno audio datoteka: {audioFiles.Count}");

foreach (var file in audioFiles)
{
    await minio.UploadAudioAsync(
        file,
        45.815,
        15.982);
}

Console.WriteLine(
    "Sve audio datoteke su obrađene.");

var classifier =
    new BirdClassificationService(
        database,
        minio);

foreach (var file in audioFiles)
{
    var result =
        await classifier.ClassifyAsync(file);

    foreach (var bird in result.Results)
    {
        Console.WriteLine(
            $"  {bird.CommonName} - " +
            $"{bird.ScientificName} - " +
            $"confidence: {bird.Confidence}");
    }
}

Console.WriteLine();
Console.WriteLine(
    "Pritisni Enter za zatvaranje...");

Console.ReadLine();
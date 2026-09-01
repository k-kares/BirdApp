using BirdApp.Classification;
using BirdApp.Database;
using BirdApp.Kafka;
using BirdApp.Minio;
using BirdApp.Scraping;
using System.Net.Http.Headers;

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

var filePath = "Audio/Agelastes_meleagrides.mp3";

using var client = new HttpClient();

using var form = new MultipartFormDataContent();

using var fileStream = File.OpenRead(filePath);

using var fileContent = new StreamContent(fileStream);

fileContent.Headers.ContentType =
    new MediaTypeHeaderValue("audio/mpeg");

form.Add(
    fileContent,
    "file",
    Path.GetFileName(filePath));

Console.WriteLine("Šaljem audio prema API-ju...");

var response = await client.PostAsync(
    "https://aves.regoch.net/api/classify",
    form);

Console.WriteLine($"HTTP status: {(int)response.StatusCode}");
Console.WriteLine($"Status: {response.StatusCode}");

var responseBody = await response.Content.ReadAsStringAsync();

Console.WriteLine("Odgovor API-ja:");
Console.WriteLine(responseBody);

var classifier = new BirdClassificationService();

foreach (var file in audioFiles)
{
    var result = await classifier.ClassifyAsync(file);

    foreach (var bird in result.Results)
    {
        Console.WriteLine(
            $"  {bird.CommonName} - " +
            $"{bird.ScientificName} - " +
            $"confidence: {bird.Confidence}");
    }
}

Console.WriteLine();
Console.WriteLine("Pritisni Enter za zatvaranje...");
Console.ReadLine();
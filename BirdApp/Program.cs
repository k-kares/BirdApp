using BirdApp.Scraping;
using BirdApp.Database;

var scraper = new BirdScraper();

var birds = await scraper.ScrapeAsync(50);

Console.WriteLine();
Console.WriteLine($"Ukupno obrađeno ptica: {birds.Count}");

var database = new MongoDbService();

await database.SaveBirdsAsync(birds);

Console.WriteLine();
Console.WriteLine("Pritisni Enter za zatvaranje...");
Console.ReadLine();
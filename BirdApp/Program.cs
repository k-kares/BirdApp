using BirdApp.Scraping;

var scraper = new BirdScraper();

var birds = await scraper.ScrapeAsync(3);

Console.WriteLine();
Console.WriteLine($"Ukupno obrađeno ptica: {birds.Count}");

Console.WriteLine();
Console.WriteLine("Pritisni Enter za zatvaranje...");
Console.ReadLine();
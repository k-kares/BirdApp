using BirdApp.Models;
using Microsoft.Playwright;

using var playwright = await Playwright.CreateAsync();

await using var browser = await playwright.Chromium.LaunchAsync(
    new BrowserTypeLaunchOptions
    {
        Headless = false,
        Channel = "chrome"
    });

var page = await browser.NewPageAsync();

await page.GotoAsync("https://aves.regoch.net/index.html");

await page.Locator("a[href*='details.html']").First.WaitForAsync();

var birds = new List<Bird>();

const int pagesToScrape = 50;

for (int currentPage = 1; currentPage <= pagesToScrape; currentPage++)
{
    Console.WriteLine($"\n========== STRANICA {currentPage}/{pagesToScrape} ==========");

    // 1. Uzmi sve linkove ptica s trenutne stranice
    var links = page.Locator("a[href*='details.html']");
    int count = await links.CountAsync();

    Console.WriteLine($"Pronađeno linkova: {count}");

    var birdLinks = new List<string>();

    for (int i = 0; i < count; i++)
    {
        string href = await links.Nth(i).GetAttributeAsync("href") ?? "";
        birdLinks.Add(href);
    }

    // 2. Obradi svaku pticu
    foreach (string href in birdLinks)
    {
        await page.GotoAsync($"https://aves.regoch.net/{href}");

        async Task<string> GetValue(string label)
        {
            return await page
                .Locator("dt", new PageLocatorOptions { HasText = label })
                .Locator("xpath=following-sibling::dd[1]")
                .InnerTextAsync();
        }

        var bird = new Bird
        {
            ScientificName = await GetValue("Scientific Name:"),
            CanonicalName = await GetValue("Canonical Name:"),
            Rank = await GetValue("Rank:"),
            Kingdom = await GetValue("Kingdom:"),
            Phylum = await GetValue("Phylum:"),
            Class = await GetValue("Class:"),
            Order = await GetValue("Order:"),
            Family = await GetValue("Family:"),
            Genus = await GetValue("Genus:")
        };

        birds.Add(bird);

        Console.WriteLine($"{birds.Count}. {bird.CanonicalName}");
    }

    // 3. Ako ima još stranica, vrati se na index
    //    i klikni Next
    if (currentPage < pagesToScrape)
    {
        await page.GotoAsync("https://aves.regoch.net/index.html");

        await page
            .Locator("a[href*='details.html']")
            .First
            .WaitForAsync();

        var nextButton = page.Locator(
            "button",
            new PageLocatorOptions { HasText = "Next" });

        await nextButton.ClickAsync();
    }
}

Console.WriteLine($"\n================================");
Console.WriteLine($"Ukupno obrađeno ptica: {birds.Count}");
Console.WriteLine($"================================");

Console.WriteLine("\nPritisni Enter za zatvaranje...");
Console.ReadLine();

await browser.CloseAsync();
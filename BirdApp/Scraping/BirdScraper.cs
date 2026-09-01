using BirdApp.Models;
using Microsoft.Playwright;

namespace BirdApp.Scraping;

public class BirdScraper
{
    public async Task<List<Bird>> ScrapeAsync(int pagesToScrape)
    {
        using var playwright = await Playwright.CreateAsync();

        await using var browser =
            await playwright.Chromium.LaunchAsync(
                new BrowserTypeLaunchOptions
                {
                    Headless = false,
                    Channel = "chrome"
                });

        var page = await browser.NewPageAsync();

        // ============================================
        // FAZA 1: SAKUPLJANJE SVIH HREFOVA
        // ============================================

        var birdLinks = new List<string>();

        await page.GotoAsync(
            "https://aves.regoch.net/index.html");

        await page
            .Locator("a[href*='details.html']")
            .First
            .WaitForAsync();

        for (int currentPage = 1;
             currentPage <= pagesToScrape;
             currentPage++)
        {
            Console.WriteLine(
                $"Sakupljam linkove: stranica {currentPage}/{pagesToScrape}");

            var links = page.Locator(
                "a[href*='details.html']");

            int count = await links.CountAsync();

            Console.WriteLine(
                $"  Pronađeno linkova: {count}");

            for (int i = 0; i < count; i++)
            {
                string href =
                    await links.Nth(i)
                        .GetAttributeAsync("href")
                    ?? "";

                if (!string.IsNullOrWhiteSpace(href))
                {
                    birdLinks.Add(href);
                }
            }

            // Idemo na sljedeću stranicu,
            // ali NE otvaramo ponovno index.html.
            if (currentPage < pagesToScrape)
            {
                var nextButton = page.Locator(
                    "button",
                    new PageLocatorOptions
                    {
                        HasText = "Next"
                    });

                await nextButton.ClickAsync();

                await page.WaitForTimeoutAsync(300);
            }
        }

        Console.WriteLine();
        Console.WriteLine(
            $"================================");

        Console.WriteLine(
            $"Ukupno pronađeno linkova: {birdLinks.Count}");

        Console.WriteLine(
            $"================================");


        // ============================================
        // FAZA 2: SCRAPING DETAILS STRANICA
        // ============================================

        var birds = new List<Bird>();

        for (int i = 0; i < birdLinks.Count; i++)
        {
            string href = birdLinks[i];

            Console.WriteLine();
            Console.WriteLine(
                $"Obrađujem pticu {i + 1}/{birdLinks.Count}: {href}");

            await page.GotoAsync(
                $"https://aves.regoch.net/{href}");

            string idText = href.Split("id=")[1];

            long taxonId = long.Parse(idText);

            async Task<string> GetValue(string label)
            {
                return await page
                    .Locator(
                        "dt",
                        new PageLocatorOptions
                        {
                            HasText = label
                        })
                    .Locator(
                        "xpath=following-sibling::dd[1]")
                    .InnerTextAsync();
            }

            var bird = new Bird
            {
                TaxonId = taxonId,

                ScientificName =
                    await GetValue("Scientific Name:"),

                CanonicalName =
                    await GetValue("Canonical Name:"),

                Rank =
                    await GetValue("Rank:"),

                Kingdom =
                    await GetValue("Kingdom:"),

                Phylum =
                    await GetValue("Phylum:"),

                Class =
                    await GetValue("Class:"),

                Order =
                    await GetValue("Order:"),

                Family =
                    await GetValue("Family:"),

                Genus =
                    await GetValue("Genus:")
            };

            birds.Add(bird);

            Console.WriteLine(
                $"  {bird.CanonicalName}");
        }

        await browser.CloseAsync();

        return birds;
    }
}
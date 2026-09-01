using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http.Headers;
using System.Text.Json;
using BirdApp.Models;

namespace BirdApp.Classification;

public class BirdClassificationService
{
    private readonly HttpClient _httpClient;

    private const string ApiUrl =
        "https://aves.regoch.net/api/classify";

    public BirdClassificationService()
    {
        _httpClient = new HttpClient();
    }

    public async Task<ClassificationResponse> ClassifyAsync(
        string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(
                "Audio datoteka ne postoji.",
                filePath);
        }

        using var form = new MultipartFormDataContent();

        using var fileStream = File.OpenRead(filePath);

        using var fileContent =
            new StreamContent(fileStream);

        var extension =
            Path.GetExtension(filePath)
                .ToLowerInvariant();

        var contentType = extension switch
        {
            ".mp3" => "audio/mpeg",
            ".wav" => "audio/wav",
            _ => "application/octet-stream"
        };

        fileContent.Headers.ContentType =
            new MediaTypeHeaderValue(contentType);

        form.Add(
            fileContent,
            "file",
            Path.GetFileName(filePath));

        Console.WriteLine(
            $"Šaljem klasifikaciju: {Path.GetFileName(filePath)}");

        var response = await _httpClient.PostAsync(
            ApiUrl,
            form);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();

            Console.WriteLine(
                $"Klasifikacija nije uspjela za {Path.GetFileName(filePath)}");

            Console.WriteLine(
                $"HTTP status: {(int)response.StatusCode} {response.StatusCode}");

            Console.WriteLine(
                $"Odgovor API-ja: {error}");

            return new ClassificationResponse();
        }

        var json =
            await response.Content.ReadAsStringAsync();

        var result =
            JsonSerializer.Deserialize<ClassificationResponse>(
                json);

        if (result == null)
        {
            throw new Exception(
                "API je vratio neispravan odgovor.");
        }

        Console.WriteLine(
            $"Pronađeno rezultata: {result.Results.Count}");

        return result;
    }
}

using BirdApp.Database;
using BirdApp.Minio;
using BirdApp.Models;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace BirdApp.Classification;

public class BirdClassificationService
{
    private readonly HttpClient _httpClient;
    private readonly MinioService _minio;

    private const string ApiUrl =
        "https://aves.regoch.net/api/classify";

    private readonly MongoDbService _database;

    public BirdClassificationService(
    MongoDbService database,
    MinioService minio)
    {
        _database = database;
        _minio = minio;
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

        var requestTime = DateTime.UtcNow;

        var response = await _httpClient.PostAsync(
            ApiUrl,
            form);

        if (!response.IsSuccessStatusCode)
        {
            var error =
                await response.Content.ReadAsStringAsync();

            Console.WriteLine(
                $"Klasifikacija nije uspjela za " +
                $"{Path.GetFileName(filePath)}");

            Console.WriteLine(
                $"HTTP status: {(int)response.StatusCode} " +
                $"{response.StatusCode}");

            Console.WriteLine(
                $"Odgovor API-ja: {error}");

            var errorLog = new
            {
                FileName = Path.GetFileName(filePath),
                RequestTime = requestTime,
                StatusCode = (int)response.StatusCode,
                Status = response.StatusCode.ToString(),
                ResultsCount = 0,
                Error = error
            };

            await _minio.UploadClassificationLogAsync(
                Path.GetFileName(filePath),
                JsonSerializer.Serialize(
                    errorLog,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    }));

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

        var successLog = new
        {
            FileName = Path.GetFileName(filePath),
            RequestTime = requestTime,
            StatusCode = (int)response.StatusCode,
            Status = response.StatusCode.ToString(),
            ResultsCount = result.Results.Count
        };

        await _minio.UploadClassificationLogAsync(
            Path.GetFileName(filePath),
            JsonSerializer.Serialize(
                successLog,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                }));

        Console.WriteLine(
            $"Pronađeno rezultata: {result.Results.Count}");

        var fileName = Path.GetFileName(filePath);

        var audioFile = await _database.GetAudioFileAsync(fileName);

        if (audioFile == null)
        {
            Console.WriteLine(
                $"Audio metadata nije pronađena: {fileName}");

            return result;
        }

        foreach (var item in result.Results)
        {
            var bird =
                await _database.GetBirdByCanonicalNameAsync(
                    item.ScientificName);

            Console.WriteLine(
                 $"Tražim vrstu: '{item.ScientificName}'");

            if (bird == null)
            {
                Console.WriteLine(
                    $"NIJE pronađena u birds: '{item.ScientificName}'");
            }
            else
            {
                Console.WriteLine(
                    $"PRONAĐENA: '{bird.ScientificName}', " +
                    $"TaxonId={bird.TaxonId}");
            }

            var classification = new AudioClassification
            {
                AudioFileId = audioFile.Id,
                FileName = audioFile.FileName,
                CommonName = item.CommonName,
                ScientificName = item.ScientificName,
                StartTime = item.StartTime,
                EndTime = item.EndTime,
                Confidence = item.Confidence,
                Label = item.Label
            };

            if (bird != null)
            {
                classification.TaxonId = bird.TaxonId;

                classification.ObservationIds =
                    await _database.GetObservationIdsByTaxonIdAsync(
                        bird.TaxonId);

                Console.WriteLine(
                    $"Povezano s vrstom: {bird.ScientificName}, " +
                    $"TaxonId={bird.TaxonId}, " +
                    $"opažanja={classification.ObservationIds.Count}");
            }
            else
            {
                Console.WriteLine(
                    $"Vrsta nije pronađena u birds: " +
                    $"{item.ScientificName}");
            }

            await _database.SaveAudioClassificationAsync(
                classification);
        }

        return result;
    }
}

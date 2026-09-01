using BirdApp.Database;
using Minio;
using Minio.DataModel.Args;

namespace BirdApp.Minio;

public class MinioService
{
    private readonly IMinioClient _client;
    private readonly MongoDbService _database;

    private const string BucketName = "bird-audio";

    public MinioService(MongoDbService database)
    {
        _database = database;

        _client = new MinioClient()
            .WithEndpoint("localhost:9000")
            .WithCredentials("minioadmin", "minioadmin")
            .Build();
    }

    public async Task UploadAudioAsync(
        string filePath,
        double latitude,
        double longitude)
    {
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"Datoteka ne postoji: {filePath}");
            return;
        }

        var fileName = Path.GetFileName(filePath);

        bool bucketExists = await _client.BucketExistsAsync(
            new BucketExistsArgs()
                .WithBucket(BucketName));

        if (!bucketExists)
        {
            await _client.MakeBucketAsync(
                new MakeBucketArgs()
                    .WithBucket(BucketName));
        }

        await _client.PutObjectAsync(
            new PutObjectArgs()
                .WithBucket(BucketName)
                .WithObject(fileName)
                .WithFileName(filePath));

        var audioFile = new BirdApp.Models.AudioFile
        {
            FileName = fileName,
            ObjectName = fileName,
            BucketName = BucketName,
            Latitude = latitude,
            Longitude = longitude
        };

        await _database.SaveAudioFileAsync(audioFile);

        Console.WriteLine(
            $"Uploadano u MinIO i spremljeno u MongoDB: {fileName}");
    }
}
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace BirdApp.Models;

public class AudioFile
{
    [BsonId]
    public ObjectId Id { get; set; }

    public string FileName { get; set; } = "";

    public string ObjectName { get; set; } = "";

    public string BucketName { get; set; } = "";

    public double Latitude { get; set; }

    public double Longitude { get; set; }
}

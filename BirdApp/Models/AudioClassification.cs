using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace BirdApp.Models;

public class AudioClassification
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public ObjectId AudioFileId { get; set; }

    public string FileName { get; set; } = "";

    public long? TaxonId { get; set; }

    public List<ObjectId> ObservationIds { get; set; } = new();

    public string CommonName { get; set; } = "";

    public string ScientificName { get; set; } = "";

    public double StartTime { get; set; }

    public double EndTime { get; set; }

    public double Confidence { get; set; }

    public string Label { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace BirdApp.Models;

public class ClassificationResponse
{
    [JsonPropertyName("results")]
    public List<ClassificationResult> Results { get; set; } = new();
}

public class ClassificationResult
{
    [JsonPropertyName("common_name")]
    public string CommonName { get; set; } = "";

    [JsonPropertyName("scientific_name")]
    public string ScientificName { get; set; } = "";

    [JsonPropertyName("start_time")]
    public double StartTime { get; set; }

    [JsonPropertyName("end_time")]
    public double EndTime { get; set; }

    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }

    [JsonPropertyName("label")]
    public string Label { get; set; } = "";
}
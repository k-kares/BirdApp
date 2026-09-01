using System;
using System.Collections.Generic;
using System.Text;

namespace BirdApp.Models;

public class AudioClassification
{
    public string AudioFileId { get; set; } = "";

    public string FileName { get; set; } = "";

    public string CommonName { get; set; } = "";

    public string ScientificName { get; set; } = "";

    public double StartTime { get; set; }

    public double EndTime { get; set; }

    public double Confidence { get; set; }

    public string Label { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
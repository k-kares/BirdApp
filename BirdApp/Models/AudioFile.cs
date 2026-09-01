using System;
using System.Collections.Generic;
using System.Text;

namespace BirdApp.Models;

public class AudioFile
{
    public string FileName { get; set; } = "";

    public string ObjectName { get; set; } = "";

    public string BucketName { get; set; } = "";

    public double Latitude { get; set; }

    public double Longitude { get; set; }
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace BirdApp.Models
{
    public class Observation
    {
        public long TaxonId { get; set; }

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public Dictionary<string, JsonElement> BiologicalData { get; set; } = new();
    }
}

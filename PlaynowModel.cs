using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace WatchWave.Models
{
    public class PlaynowResults
    {
        [JsonProperty("iso_639_1")] // If the API uses a different name for the property
        public string Iso6391 { get; set; }

        [JsonProperty("iso_3166_1")]
        public string Iso31661 { get; set; }

        public string Name { get; set; }
        public string Key { get; set; }
        public string Site { get; set; }
        public int Size { get; set; }
        public string Type { get; set; }
        public bool Official { get; set; }

        [JsonProperty("published_at")] // DateTime format correction for JSON parsing
        public DateTime PublishedAt { get; set; }

        public string Id { get; set; }
    }

    public class PlaynowRoot
    {
        public int Id { get; set; }

        [JsonProperty("results")] // Ensure the API response's property name matches
        public List<PlaynowResults> Results { get; set; }

        [JsonProperty("api_fetched")] // For correct deserialization of bool if the API uses a different key
        public bool API_Fetched { get; set; }
    }
}

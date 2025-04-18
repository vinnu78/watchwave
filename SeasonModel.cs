namespace WatchWave.Models
{
    public class SeasonResults
    {
        public string? Backdrop_Path { get; set; }
        public string? First_Air_Date { get; set; }
        public List<int> Genre_Ids { get; set; } = new List<int>();
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<string> Origin_Country { get; set; } = new List<string>();
        public string Original_Language { get; set; } = string.Empty;
        public string Original_Name { get; set; } = string.Empty;
        public string? Overview { get; set; }
        public double Popularity { get; set; }
        public string? Poster_Path { get; set; }
        public double Vote_Average { get; set; }
        public int Vote_Count { get; set; }
    }

    public class SeasonRoot
    {
        public int Page { get; set; }
        public List<SeasonResults> Results { get; set; } = new List<SeasonResults>();
        public int Total_Pages { get; set; }
        public int Total_Results { get; set; }
        public bool API_Fetched { get; set; }
    }
}
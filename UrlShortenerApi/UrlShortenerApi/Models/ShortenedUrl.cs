namespace UrlShortenerApi.Models
{
    public class ShortenedUrl
    {
        public int Id { get; set; }  

        public string ShortCode { get; set; } = string.Empty;

        public string OriginalUrl { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }  
    }
}

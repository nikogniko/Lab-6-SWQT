namespace SnippetsLibraryWebApp.Models
{
    public class AuthorModel
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public int? SnippetsCount { get; set; }
    }
}

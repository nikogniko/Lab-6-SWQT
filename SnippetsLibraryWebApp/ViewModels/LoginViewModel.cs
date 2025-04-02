namespace SnippetsLibraryWebApp.ViewModels
{
    public class LoginViewModel
    {
        public string __RequestVerificationToken { get; set; }
        public string email { get; set; }
        public string password { get; set; }
        public bool RememberMe { get; set; }
    }
}

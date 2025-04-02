using System.ComponentModel.DataAnnotations;

namespace SnippetsLibraryWebApp.ViewModels
{
    public class RegisterViewModel
    {
        public string __RequestVerificationToken { get; set; }
        public string username { get; set; }
        public string email { get; set; }
        public string password { get; set; }
    }
}

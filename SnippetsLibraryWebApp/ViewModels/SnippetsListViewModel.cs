using Microsoft.AspNetCore.Mvc.Rendering;
using SnippetsLibraryWebApp.Models;

namespace SnippetsLibraryWebApp.ViewModels
{
    public class SnippetsListViewModel
    {
        // Список сніпетів
        public IEnumerable<SnippetModel> Snippets { get; set; }

        // Фільтри
        public List<SelectListItem> Authors { get; set; }
        public List<SelectListItem> Categories { get; set; }
        public List<SelectListItem> Tags { get; set; }
        public List<SelectListItem> ProgrammingLanguages { get; set; }

        // Поточні вибрані фільтри
        public int? SelectedAuthorId { get; set; }
        public List<int> SelectedCategoryIds { get; set; }
        public List<int> SelectedTagIds { get; set; }
        public int? SelectedProgrammingLanguageId { get; set; }

        // Пошуковий рядок
        public string SearchQuery { get; set; }
    }
}

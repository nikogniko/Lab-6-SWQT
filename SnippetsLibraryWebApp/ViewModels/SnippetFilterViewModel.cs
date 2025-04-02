using Azure;
using SnippetsLibraryWebApp.Models;

namespace SnippetsLibraryWebApp.ViewModels
{
    public class SnippetFilterViewModel
    {
        // Фільтри
        public int? AuthorId { get; set; }
        public int? CategoryId { get; set; }
        public int? TagId { get; set; }
        public int? LanguageId { get; set; }
        public string SearchTitle { get; set; }

        // Списки для фільтрації
        public IEnumerable<UserModel> Authors { get; set; }
        public IEnumerable<CategoryModel> Categories { get; set; }
        public IEnumerable<TagModel> Tags { get; set; }
        public IEnumerable<ProgrammingLanguageModel> ProgrammingLanguages { get; set; }

        // Список сніпетів
        public IEnumerable<SnippetModel> Snippets { get; set; }
    }
}

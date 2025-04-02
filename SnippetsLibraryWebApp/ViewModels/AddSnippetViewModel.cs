using Azure;
using Microsoft.AspNetCore.Mvc.Rendering;
using SnippetsLibraryWebApp.Models;

namespace SnippetsLibraryWebApp.ViewModels
{
    public class EditSnippetViewModel
    {
        public int ID { get; set; }
        public string Title { get; set; }
        public int ProgrammingLanguageID { get; set; }
        public string Description { get; set; }
        public string Code { get; set; }
        public string Status { get; set; }
        public List<CategoryModel> Categories { get; set; }
        public List<TagModel> Tags { get; set; }

        // SelectLists для випадаючих списків
        public SelectList ProgrammingLanguages { get; set; }
        public List<SelectListItem> AllCategories { get; set; }
        public List<SelectListItem> AllTags { get; set; }
    }
}

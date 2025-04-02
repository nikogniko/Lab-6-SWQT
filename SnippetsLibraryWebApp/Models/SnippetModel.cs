namespace SnippetsLibraryWebApp.Models
{
    using System;
    using System.Collections.Generic;

    public class SnippetModel
    {
        // Властивості для Snippet
        public int ID { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int ProgrammingLanguageID { get; set; }
        public int AuthorID { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string Code { get; set; }

        private string _status;
        public string Status
        {
            get => _status;
            set
            {
                // Дозволяє тільки "Public" або "Private"
                if (value.ToLower() == "public" || value.ToLower() == "private")
                {
                    _status = value.ToLower();
                }
                else
                {
                    throw new ArgumentException("Status can only be 'public' or 'private'.");
                }
            }
        }

        public int SavesCount { get; set; }

        // Список категорій
        public IEnumerable<CategoryModel> Categories { get; set; } = new List<CategoryModel>();

        // Список тегів
        public IEnumerable<TagModel> Tags { get; set; } = new List<TagModel>();
    }
}

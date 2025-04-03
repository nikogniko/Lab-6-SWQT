using Duende.IdentityServer.Events;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json.Linq;
using SnippetsLibraryWebApp.Extensions;
using SnippetsLibraryWebApp.Models;
using SnippetsLibraryWebApp.Repository;
using SnippetsLibraryWebApp.Utils;
using SnippetsLibraryWebApp.ViewModels;
using System.Diagnostics;
using System.Security.Claims;

namespace SnippetsLibraryWebApp.Controllers
{
    [Authorize(AuthenticationSchemes = "CookieAuth")]
    public class SnippetsController : Controller
    {
        private readonly SnippetRepository _snippetsRepository;
        private readonly TagRepository _tagRepository;
        private readonly CategoryRepository _categoryRepository;
        private readonly ProgrammingLanguageRepository _programmingLanguageRepository;
        private readonly UserRepository _userRepository;


        public SnippetsController(
            SnippetRepository snippetRepository, 
            TagRepository tagRepository, 
            CategoryRepository categoryRepository, 
            ProgrammingLanguageRepository programmingLanguageRepository,
            UserRepository userRepository)
        {
            _snippetsRepository = snippetRepository;
            _tagRepository = tagRepository;
            _categoryRepository = categoryRepository;
            _programmingLanguageRepository = programmingLanguageRepository;
            _userRepository = userRepository;
        }

        // Дія для відкриття сторінки додавання сніпета
        [HttpGet]
        public IActionResult AddSnippet()
        {
            return View("~/Views/AddSnippet/AddSnippet.cshtml");
        }

        // Метод для пошуку авторів
        [HttpGet]
        [Route("Snippets/SearchAuthors")]
        [AllowAnonymous]
        public async Task<IActionResult> SearchAuthors(string query)
        {
            var authors = await _userRepository.GetAllAuthors(query);
            return Json(authors);
        }

        [HttpGet]
        [Route("Snippets/SearchTags")]
        [AllowAnonymous]
        public async Task<IActionResult> SearchTags(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest("Query cannot be empty.");
            }

            var tags = await _tagRepository.SearchTagsAsync(query);
            return Json(tags);
        }

        [AllowAnonymous]
        public async Task<IActionResult> AllSnippets(int[] authorIds, int[] categoryIds, int[] tagIds, int[] languageIds, string sortOrder)
        {
            var snippets = await _snippetsRepository.GetFilteredSnippetsAsync(authorIds, categoryIds, tagIds, languageIds, sortOrder);

            // Передача всіх категорій та мов програмування для випадаючих списків
            var allTags = await _tagRepository.SearchTagsAsync();
            var allCategories = await _categoryRepository.GetAllCategoriesAsync();
            var allLanguages = await _programmingLanguageRepository.GetAllProgrammingLanguagesAsync();

            ViewBag.Tags = allTags;
            ViewBag.Categories = allCategories;
            ViewBag.ProgrammingLanguages = allLanguages;

            ViewBag.PageType = nameof(AllSnippets);

            if (Request.IsAjaxRequest())
            {
                return PartialView("_SnippetList", snippets);
            }

            return View(snippets);
        }

        // Дія для перегляду деталей сніпета
        [AllowAnonymous] // Дозволяє анонімний доступ до деталей, якщо сніпети публічні
        [HttpGet]
        [Route("Snippets/Details/{id}")]
        public async Task<IActionResult> Details(int id)
        {
            var snippet = await _snippetsRepository.GetSnippetByIdAsync(id);
            if (snippet == null)
            {
                return NotFound();
            }

            // Припустимо, що Tags та Categories вже завантажені в snippet
            return View(snippet);
        }

        // Додайте ці методи у SnippetsController

        // Метод для видалення сніпета (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("/Snippets/DeleteSnippet")]
        public async Task<IActionResult> DeleteSnippet(int ID)
        {
            var snippet = await _snippetsRepository.GetSnippetByIdAsync(ID);
            if (snippet == null)
            {
                return NotFound();
            }

            // Перевірка, чи користувач є власником сніпета
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            if (snippet.AuthorID != currentUserId)
            {
                return Forbid();
            }

            // Видалення сніпета
            await _snippetsRepository.DeleteSnippetAsync(ID, currentUserId);

            return RedirectToAction("AllSnippets");
        }

        // GET: Snippets/EditSnippet/5
        [HttpGet]
        public async Task<IActionResult> EditSnippet(int id)
        {
            var snippet = await _snippetsRepository.GetSnippetByIdAsync(id);
            if (snippet == null)
            {
                return NotFound();
            }

            // Перевірка, чи користувач є власником сніпета
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            if (snippet.AuthorID != currentUserId)
            {
                return Forbid();
            }

            var viewModel = new EditSnippetViewModel
            {
                ID = snippet.ID,
                Title = snippet.Title,
                ProgrammingLanguageID = snippet.ProgrammingLanguageID,
                Description = snippet.Description,
                Code = snippet.Code,
                Status = snippet.Status,
                Categories = snippet.Categories.ToList(),
                Tags = snippet.Tags.ToList(),
                ProgrammingLanguages = new SelectList(await _programmingLanguageRepository.GetAllProgrammingLanguagesAsync(), "ID", "Name", snippet.ProgrammingLanguageID),
                AllCategories = (await _categoryRepository.GetAllCategoriesAsync()).Select(c => new SelectListItem
                {
                    Value = c.ID.ToString(),
                    Text = c.Name,
                    Selected = snippet.Categories.Any(sc => sc.ID == c.ID)
                }).ToList(),
                AllTags = (await _tagRepository.SearchTagsAsync()).Select(t => new SelectListItem
                {
                    Value = t.ID.ToString(),
                    Text = t.Name,
                    Selected = snippet.Tags.Any(st => st.ID == t.ID)
                }).ToList()
            };

            return View(viewModel);
        }

        // Метод для обробки редагування сніпета
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSnippet(EditSnippetViewModel model, int[] selectedCategories, int[] selectedTags)
        {
            if (!ModelState.IsValid)
            {
                await PopulateCategoriesAndTagsAsync(model, selectedCategories, selectedTags);
            }

            var existingSnippet = await _snippetsRepository.GetSnippetByIdAsync(model.ID);
            if (existingSnippet == null)
            {
                return Forbid();
            }

            bool areCategoriesChanged = CollectionsChanged(existingSnippet.Categories, model.Categories);
            bool areTagsChanged = CollectionsChanged(existingSnippet.Tags, model.Tags);

            UpdateSnippet(existingSnippet, model);

            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            bool result = await _snippetsRepository.UpdateSnippetAsync(existingSnippet, userId, areCategoriesChanged, areTagsChanged);

            return Json(new { success = result, formType = "edit" });

            // Локальний метод для завантаження категорій та тегів
            async Task PopulateCategoriesAndTagsAsync(EditSnippetViewModel m, int[] selCategories, int[] selTags)
            {
                if (m.Categories == null || !m.Categories.Any())
                {
                    m.Categories = new List<CategoryModel>();
                    if (selCategories != null && selCategories.Length > 0)
                    {
                        foreach (var categoryId in selCategories)
                        {
                            m.Categories.Add(await _categoryRepository.GetCategoryByIdAsync(categoryId));
                        }
                    }
                }

                if (m.Tags == null || !m.Tags.Any())
                {
                    m.Tags = new List<TagModel>();
                    if (selTags != null && selTags.Length > 0)
                    {
                        foreach (var tagId in selTags)
                        {
                            m.Tags.Add(await _tagRepository.GetTagsByIdAsync(tagId));
                        }
                    }
                }
            }

            // Локальний метод для порівняння колекцій за ID
            bool CollectionsChanged<T>(IEnumerable<T> existing, IEnumerable<T> updated) where T : class
            {
                var existingIds = existing.Select(e => (int)typeof(T).GetProperty("ID").GetValue(e));
                var updatedIds = updated.Select(u => (int)typeof(T).GetProperty("ID").GetValue(u));
                return !existingIds.SequenceEqual(updatedIds);
            }

            // Локальний метод для оновлення полів сніпета
            void UpdateSnippet(Snippet existing, EditSnippetViewModel m)
            {
                existing.Title = m.Title;
                existing.Description = m.Description;
                existing.ProgrammingLanguageID = m.ProgrammingLanguageID;
                existing.Status = m.Status;
                existing.Code = m.Code;
                existing.UpdatedAt = DateTime.UtcNow;
                existing.Tags = m.Tags;
                existing.Categories = m.Categories;
            }
        }

        [HttpPost]
        [Route("/Snippets/AddSnippetToSavedAsync")]
        public async Task<IActionResult> AddSnippetToSavedAsync(int userId, int snippetId)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Unauthorized(); // Повертає статус 401
            }

            var isAlreadySaved = await _snippetsRepository.IsSnippedSavedByUser(userId, snippetId);

            if (isAlreadySaved)
            {
                var result = await _snippetsRepository.RemoveSnippetFromSavedAsync(userId, snippetId);

                if (!result)
                {
                    return Json(new { success = false, isSaved = true });
                }

                return Json(new { success = true, isSaved = false });
            }
            else
            {
                var result = await _snippetsRepository.AddSnippetToSavedAsync(userId, snippetId);

                if (!result)
                {
                    return Json(new { success = false, isSaved = false });
                }

                return Json(new { success = true, isSaved = true });
            }
        }

        // Дія для відображення обраних сніпетів користувача
        [HttpGet]
        [Route("Snippets/FavoriteSnippets")]
        [AllowAnonymous]
        public async Task<IActionResult> FavoriteSnippets(int[] authorIds, int[] categoryIds, int[] tagIds, int[] languageIds, string sortOrder)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Unauthorized(); // Повертає статус 401
            }

            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var snippets = await _snippetsRepository.GetFavoriteSnippetsAsync(userId, authorIds, categoryIds, tagIds, languageIds, sortOrder);

            // Передача всіх категорій та мов програмування для випадаючих списків
            var allTags = await _tagRepository.SearchTagsAsync();
            var allCategories = await _categoryRepository.GetAllCategoriesAsync();
            var allLanguages = await _programmingLanguageRepository.GetAllProgrammingLanguagesAsync();

            ViewBag.Tags = allTags;
            ViewBag.Categories = allCategories;
            ViewBag.ProgrammingLanguages = allLanguages;

            ViewBag.PageType = nameof(FavoriteSnippets);

            if (Request.IsAjaxRequest())
            {
                return PartialView("_SnippetList", snippets);
            }

            return View(snippets);
        }

        // Дія для відображення авторських сніпетів користувача
        [HttpGet]
        [Route("Snippets/MySnippets")]
        public async Task<IActionResult> MySnippets(int[] authorIds, int[] categoryIds, int[] tagIds, int[] languageIds, string sortOrder)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Unauthorized(); // Повертає статус 401
            }

            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var snippets = await _snippetsRepository.GetMySnippetsAsync(userId, authorIds, categoryIds, tagIds, languageIds, sortOrder);

            // Передача всіх категорій та мов програмування для випадаючих списків
            var allTags = await _tagRepository.SearchTagsAsync();
            var allCategories = await _categoryRepository.GetAllCategoriesAsync();
            var allLanguages = await _programmingLanguageRepository.GetAllProgrammingLanguagesAsync();

            ViewBag.Tags = allTags;
            ViewBag.Categories = allCategories;
            ViewBag.ProgrammingLanguages = allLanguages;

            ViewBag.PageType = nameof(MySnippets);

            if (Request.IsAjaxRequest())
            {
                return PartialView("_SnippetList", snippets);
            }

            return View(snippets);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpPost]
        [Route("api/tags/add")]
        public async Task<IActionResult> AddTag([FromBody] string tagName)
        {
            if (string.IsNullOrWhiteSpace(tagName))
            {
                return BadRequest("Invalid tag name.");
            }

            try
            {
                // Викликаємо метод репозиторія для додавання нового тега
                var tagId = await _tagRepository.AddNewTagAsync(tagName);

                // Якщо не вдалося додати тег
                if (tagId == null)
                {
                    return StatusCode(500, "Failed to add the tag.");
                }

                // Повертаємо ID нового тега
                return Ok(tagId);
            }
            catch (Exception ex)
            {
                // Логування помилки
                Console.WriteLine($"Error while adding tag: {ex.Message}");
                return StatusCode(500, "An error occurred while adding the tag.");
            }
        }

        [HttpPost]
        [Route("/Snippets/CreateSnippetAsync")]
        public async Task<IActionResult> CreateSnippetAsync(string title, string description, int programmingLanguageID, string code,
                string status, int[] categories, int[] tags, int userID)
        {
            try
            {
                List<CategoryModel> categoriesModel = new List<CategoryModel>();

                foreach (var category in categories)
                {
                    categoriesModel.Add(await _categoryRepository.GetCategoryByIdAsync(category));
                }

                List<TagModel> tagsModel = new List<TagModel>();
                foreach (var tag in tags)
                {
                    tagsModel.Add(await _tagRepository.GetTagsByIdAsync(tag));
                }

                // Створюємо новий об'єкт SnippetModel з вхідними даними
                var newSnippet = new SnippetModel
                {
                    Title = title,
                    Description = description,
                    ProgrammingLanguageID = programmingLanguageID,
                    Code = code,
                    Status = status,
                    AuthorID = userID,
                    Categories = categoriesModel,
                    Tags = tagsModel
                };

                // Додаємо сніпет до бази даних
                var snippetId = await _snippetsRepository.AddSnippetAsync(newSnippet);

                if (snippetId.HasValue)
                {
                    return Json(new { success = true, formType = "add", SnippetID = snippetId });
                }
                else
                {
                    return Json(new { success = false, formType = "add" });
                }
            }
            catch (Exception ex)
            {
                // Логування помилки
                Console.WriteLine($"Error while adding snippet: {ex.Message}");
                return StatusCode(500, "An error occurred while adding the snippet.");
            }
        }
    }
}

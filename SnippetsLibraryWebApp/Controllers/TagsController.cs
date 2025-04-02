using Microsoft.AspNetCore.Mvc;
using SnippetsLibraryWebApp.Models;
using SnippetsLibraryWebApp.Repository;

namespace SnippetsLibraryWebApp.Controllers
{
    public class TagsController : Controller
    {
        private readonly TagRepository _tagRepository;

        public TagsController(TagRepository tagRepository)
        {
            _tagRepository = tagRepository;
        }

        [HttpPost]
        public async Task<IActionResult> AddTag(string tagName)
        {
            try
            {
                var tagId = await _tagRepository.AddNewTagAsync(tagName);

                return Ok(new { success = true, TagId = tagId });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while adding tag: {ex.Message}");
                return StatusCode(500, "An error occurred while adding the tag.");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllTags()
        {
            try
            {
                var tags = await _tagRepository.SearchTagsAsync();

                return Ok(tags);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while retrieving tags: {ex.Message}");
                return StatusCode(500, "An error occurred while retrieving tags.");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetTagById(int tagId)
        {
            try
            {
                var tag = await _tagRepository.GetTagsByIdAsync(tagId);

                if (tag == null)
                {
                    return BadRequest("Tag was not found");
                }
                else
                {
                    return Ok(tag);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while retrieving tag: {ex.Message}");
                return StatusCode(500, "An error occurred while retrieving the tag.");
            }
        }
    }
}
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnippetsLibraryWebApp.Models;
using SnippetsLibraryWebApp.Repository;
using SnippetsLibraryWebApp.ViewModels;
using System.Diagnostics;
using System.Security.Claims;

namespace SnippetsLibraryWebApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly UserRepository _userRepository;
        private readonly SnippetRepository _snippetRepository;

        public HomeController(ILogger<HomeController> logger, UserRepository userRepository, SnippetRepository snippetRepository)
        {
            _logger = logger;
            _userRepository = userRepository;
            _snippetRepository = snippetRepository;
        }

        public async Task<IActionResult> Index()
        {
            var snippets = await _snippetRepository.GetAllPublicSnippetsAsync();
            return View(snippets);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpGet]
        public async Task<IActionResult> GetUsername(int userId)
        {
            try
            {
                // ??????????? ???? ??????????? ? ??????
                string username = await _userRepository.GetUsernameByIdAsync(userId);

                if (!string.IsNullOrEmpty(username))
                {
                    return Json(new { success = true, username = username });
                }
                else
                {
                    return Json(new { success = false, message = "User not found" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }
    }
}

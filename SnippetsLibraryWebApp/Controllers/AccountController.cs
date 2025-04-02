using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Scripting;
using SnippetsLibraryWebApp.Repository;
using SnippetsLibraryWebApp.ViewModels;
using System.Security.Claims;

namespace SnippetsLibraryWebApp.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly UserRepository _userRepository;
        private const string CookieAuthScheme = "CookieAuth"; // Визначаємо константу для схеми автентифікації

        public AccountController(UserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("/Account/RegisterUser")]
        public async Task<IActionResult> RegisterUser(RegisterViewModel model)
        {
            try
            {
                // ????????? ?? ??????? ????????
                if (string.IsNullOrEmpty(model.username))
                {
                    return Json(new { success = false, message = "Username cannot be empty" });
                }

                if (string.IsNullOrEmpty(model.email))
                {
                    return Json(new { success = false, message = "Email cannot be empty" });
                }

                if (string.IsNullOrEmpty(model.password))
                {
                    return Json(new { success = false, message = "Password cannot be empty" });
                }

                // ?????? ????????????? ???????????
                int? userId = await _userRepository.AddUserAsync(model.username, model.email, model.password);

                if (userId.HasValue && Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    // Автоматичний вхід після реєстрації
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()),
                        new Claim(ClaimTypes.Name, model.username)
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthScheme);

                    var authProperties = new AuthenticationProperties
                    {
                        // Налаштування автентифікації, якщо потрібно
                    };

                    await HttpContext.SignInAsync(CookieAuthScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

                    return Json(new { success = true, message = "User registered successfully", userId = userId.Value });
                }
                else
                {
                    return Json(new { success = false, message = "Error registering user" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Валідація введених даних
                    if (string.IsNullOrEmpty(model.email))
                    {
                        return Json(new { success = false, message = "Email не може бути порожнім" });
                    }

                    if (string.IsNullOrEmpty(model.password))
                    {
                        return Json(new { success = false, message = "Пароль не може бути порожнім" });
                    }

                    // Перевірка користувача
                    var user = await _userRepository.GetUserAsync(model.email, model.password);

                    if (user == null)
                    {
                        return Json(new { success = false, message = "Невірний email або пароль" });
                    }

                    // Створення Claims
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                        new Claim(ClaimTypes.Name, user.Username),
                        new Claim(ClaimTypes.Email, user.Email)
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthScheme);

                    var authProperties = new AuthenticationProperties
                    {
                        // Налаштування автентифікації, якщо потрібно
                        IsPersistent = model.RememberMe
                    };

                    await HttpContext.SignInAsync(CookieAuthScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = true, message = "Вхід успішний", Id = user.Id });
                    }
                    else
                    {
                        // Перенаправлення на головну сторінку після успішного входу
                        return RedirectToAction("AllSnippets", "Snippets");
                    }
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = $"Error: {ex.Message}" });
                }
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = "Невірний email або пароль" });
            }

            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthScheme);
            return RedirectToAction("AllSnippets", "Snippets");
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PersonalDiary.Models;
using PersonalDiary.Services;

namespace PersonalDiary.Controllers;

public class AccountController(
    UserManager<IdentityUser> userManager,
    IJwtTokenService jwtTokenService) : Controller
{
    private const string AuthenticationCookieName = "PersonalDiary.AuthToken";

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(
        LoginViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await userManager.FindByNameAsync(model.UserName);
        if (user is null || !await userManager.CheckPasswordAsync(user, model.Password))
        {
            ModelState.AddModelError(string.Empty, "Invalid user ID or password.");
            return View(model);
        }

        var token = await jwtTokenService.CreateTokenAsync(user);
        Response.Cookies.Append(AuthenticationCookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddHours(1),
            IsEssential = true
        });

        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        return RedirectToAction("Index", "Diary");
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        Response.Cookies.Delete(AuthenticationCookieName, new CookieOptions
        {
            Secure = true,
            SameSite = SameSiteMode.Strict
        });

        return RedirectToAction("Index", "Home");
    }
}

// AG1/Controllers/AuthController.cs
using AG1.Models;
using AG1.Services;
using Microsoft.AspNetCore.Mvc;

namespace AG1.Controllers;

public class AuthController : Controller
{
    private readonly IAuthService _auth;
    public AuthController(IAuthService auth) => _auth = auth;

    [HttpGet]
    public IActionResult Login() => User.Identity?.IsAuthenticated == true
        ? RedirectToAction("Index", "Home") : View();

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var vendeur = await _auth.ValidateLoginAsync(model.Email, model.MotDePasse);
        if (vendeur == null)
        {
            ModelState.AddModelError("", "Email ou mot de passe incorrect.");
            return View(model);
        }

        await _auth.SignInAsync(HttpContext, vendeur, model.RememberMe);
        TempData["Success"] = $"Bienvenue, {vendeur.NomComplet} !";
        return RedirectToAction("Index", "Home");
    }

    public async Task<IActionResult> Logout()
    {
        await _auth.SignOutAsync(HttpContext);
        return RedirectToAction("Login");
    }

    public IActionResult AccessDenied() => View();
}

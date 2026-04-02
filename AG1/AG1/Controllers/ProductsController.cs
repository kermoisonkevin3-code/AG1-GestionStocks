// AG1/Controllers/ProductsController.cs
using AG1.Models;
using AG1.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AG1.Controllers;

[Authorize]
public class ProductsController : Controller
{
    private readonly IStockService _stock;
    public ProductsController(IStockService stock) => _stock = stock;

    private int VendeurId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // ---- Liste ----
    public async Task<IActionResult> Index(string? search, int? categorieId, string? statut)
    {
        var produits = await _stock.GetAllProduitsAsync(search, categorieId, statut);
        var cats     = await _stock.GetCategoriesAsync();
        ViewBag.Categories   = cats;
        ViewBag.Search       = search;
        ViewBag.CategorieId  = categorieId;
        ViewBag.Statut       = statut;
        return View(produits);
    }

    // ---- Détail ----
    public async Task<IActionResult> Details(int id)
    {
        var p = await _stock.GetProduitByIdAsync(id);
        if (p == null) return NotFound();
        var mouvements = await _stock.GetMouvementsAsync(id, 20);
        ViewBag.Mouvements = mouvements;
        return View(p);
    }

    // ---- Créer ----
    [Authorize(Roles = "manager,admin")]
    public async Task<IActionResult> Create()
    {
        var vm = new ProduitFormViewModel { Categories = await _stock.GetCategoriesAsync() };
        return View(vm);
    }

    [HttpPost, Authorize(Roles = "manager,admin")]
    public async Task<IActionResult> Create(ProduitFormViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            vm.Categories = await _stock.GetCategoriesAsync();
            return View(vm);
        }
        var ok = await _stock.CreateProduitAsync(vm.Produit);
        if (ok) { TempData["Success"] = "Produit créé avec succès."; return RedirectToAction("Index"); }
        TempData["Error"] = "Erreur lors de la création.";
        vm.Categories = await _stock.GetCategoriesAsync();
        return View(vm);
    }

    // ---- Modifier ----
    [Authorize(Roles = "manager,admin")]
    public async Task<IActionResult> Edit(int id)
    {
        var p = await _stock.GetProduitByIdAsync(id);
        if (p == null) return NotFound();
        return View(new ProduitFormViewModel { Produit = p, Categories = await _stock.GetCategoriesAsync() });
    }

    [HttpPost, Authorize(Roles = "manager,admin")]
    public async Task<IActionResult> Edit(ProduitFormViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            vm.Categories = await _stock.GetCategoriesAsync();
            return View(vm);
        }
        var ok = await _stock.UpdateProduitAsync(vm.Produit);
        TempData[ok ? "Success" : "Error"] = ok ? "Produit mis à jour." : "Erreur de mise à jour.";
        return RedirectToAction("Details", new { id = vm.Produit.Id });
    }

    // ---- Ajustement stock ----
    [HttpPost]
    public async Task<IActionResult> AjusterStock(StockActionViewModel vm)
    {
        if (vm.Quantite <= 0)
        {
            TempData["Error"] = "La quantité doit être supérieure à 0.";
            return RedirectToAction("Details", new { id = vm.ProduitId });
        }
        var ok = await _stock.AjusterStockAsync(vm.ProduitId, VendeurId, vm.Type, vm.Quantite, vm.Motif);
        TempData[ok ? "Success" : "Error"] = ok ? "Stock mis à jour." : "Erreur lors de la mise à jour du stock.";
        return RedirectToAction("Details", new { id = vm.ProduitId });
    }

    // ---- Toggle actif ----
    [HttpPost, Authorize(Roles = "manager,admin")]
    public async Task<IActionResult> Toggle(int id)
    {
        await _stock.ToggleProduitAsync(id);
        return RedirectToAction("Index");
    }
}

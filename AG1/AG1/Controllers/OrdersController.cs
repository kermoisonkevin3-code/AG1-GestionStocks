// AG1/Controllers/OrdersController.cs
using AG1.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AG1.Controllers;

[Authorize]
public class OrdersController : Controller
{
    private readonly ICommandeService _cmd;
    private readonly IStockService    _stock;
    public OrdersController(ICommandeService cmd, IStockService stock)
    { _cmd = cmd; _stock = stock; }

    private int VendeurId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public async Task<IActionResult> Index(string? statut, string? search)
    {
        var commandes = await _cmd.GetAllCommandesAsync(statut, search);
        ViewBag.Statut = statut;
        ViewBag.Search = search;
        return View(commandes);
    }

    public async Task<IActionResult> Details(int id)
    {
        var commande = await _cmd.GetCommandeByIdAsync(id);
        if (commande == null) return NotFound();
        return View(commande);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateStatut(int id, string statut)
    {
        var valides = new[] { "en_attente", "confirmee", "expediee", "livree", "annulee" };
        if (!valides.Contains(statut))
        {
            TempData["Error"] = "Statut invalide.";
            return RedirectToAction("Details", new { id });
        }

        // Si expédié, décrémenter le stock
        if (statut == "expediee")
        {
            var commande = await _cmd.GetCommandeByIdAsync(id);
            if (commande != null)
            {
                foreach (var ligne in commande.Lignes)
                {
                    await _stock.AjusterStockAsync(
                        ligne.ProduitId, VendeurId, "sortie",
                        ligne.Quantite, $"Expédition commande #{id}", id);
                }
            }
        }

        var ok = await _cmd.UpdateStatutAsync(id, statut, VendeurId);
        TempData[ok ? "Success" : "Error"] = ok
            ? $"Statut mis à jour : {statut}"
            : "Erreur lors de la mise à jour.";
        return RedirectToAction("Details", new { id });
    }

    public async Task<IActionResult> Clients(string? search)
    {
        var clients = await _cmd.GetClientsAsync(search);
        ViewBag.Search = search;
        return View(clients);
    }
}

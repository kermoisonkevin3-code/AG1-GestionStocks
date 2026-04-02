// AG1/Controllers/HomeController.cs
using AG1.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AG1.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly IStockService _stock;
    public HomeController(IStockService stock) => _stock = stock;

    public async Task<IActionResult> Index()
    {
        var vm = await _stock.GetDashboardDataAsync();
        return View(vm);
    }
}

using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using GorevTakip.Models;
using GorevTakip.Data;

namespace GorevTakip.Controllers;

public class HomeController : Controller
{
    private readonly DashboardRepository _repo;

    public HomeController(DashboardRepository repo)
    {
        _repo = repo;
    }

    public IActionResult Index()
    {
        // out parametreleri karşıla
        _repo.SayilariGetir(out int toplam, out int tamamlanan, out int bekleyen,
                            out int devamEden, out int gecikmis, out int bugunBiten);

        var model = new DashboardViewModel
        {
            ToplamGorev = toplam,
            TamamlananGorev = tamamlanan,
            BekleyenGorev = bekleyen,
            DevamEdenGorev = devamEden,
            GecikmisGorev = gecikmis,
            BugunBitenGorev = bugunBiten,

            KategoriDagilimlari = _repo.KategoriDagilimi(),
            YaklasanGorevler = _repo.YaklasanGorevler(5)
        };

        return View(model);
    }
}

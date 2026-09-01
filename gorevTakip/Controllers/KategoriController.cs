using Microsoft.AspNetCore.Mvc;
using GorevTakip.Data;
using GorevTakip.Models;

namespace GorevTakip.Controllers;

// [Authorize] yazmaya gerek YOK — Program.cs'teki global filtre
// zaten tüm controller'ları koruyor.
public class KategoriController : Controller
{
    private readonly KategoriRepository _repo;

    public KategoriController(KategoriRepository repo)
    {
        _repo = repo;
    }

    // GET: /Kategori
    public IActionResult Index()
    {
        return View(_repo.TumunuGetir());
    }

    // GET: /Kategori/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: /Kategori/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(Kategori kategori)
    {
        if (!ModelState.IsValid)
            return View(kategori);

        _repo.Ekle(kategori);
        TempData["Basarili"] = $"\"{kategori.KategoriAd}\" kategorisi eklendi.";

        // POST-Redirect-GET: F5'te çift kayıt olmasın
        return RedirectToAction("Index");
    }

    // GET: /Kategori/Edit/5
    public IActionResult Edit(long id)
    {
        var kategori = _repo.IdIleGetir(id);
        if (kategori == null) return NotFound();
        return View(kategori);
    }

    // POST: /Kategori/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(Kategori kategori)
    {
        if (!ModelState.IsValid)
            return View(kategori);

        _repo.Guncelle(kategori);
        TempData["Basarili"] = "Kategori güncellendi.";
        return RedirectToAction("Index");
    }

    // GET: /Kategori/Delete/5
    public IActionResult Delete(long id)
    {
        var kategori = _repo.IdIleGetir(id);
        if (kategori == null) return NotFound();

        // Silinemeyecekse kullanıcıya ÖNCEDEN söyle
        ViewBag.GorevSayisi = _repo.AktifGorevSayisi(id);
        return View(kategori);
    }

    // POST: /Kategori/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteConfirmed(long id)
    {
        // ⭐ İlişkili kayıt kontrolü
        int gorevSayisi = _repo.AktifGorevSayisi(id);

        if (gorevSayisi > 0)
        {
            TempData["Uyari"] = $"Bu kategoride {gorevSayisi} görev var. " +
                                 "Önce görevleri silmeli veya başka kategoriye taşımalısınız.";
            return RedirectToAction("Index");
        }

        _repo.PasifYap(id);
        TempData["Basarili"] = "Kategori silindi.";
        return RedirectToAction("Index");
    }

}
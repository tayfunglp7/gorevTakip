using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;   // SelectList için
using GorevTakip.Data;
using GorevTakip.Models;

namespace GorevTakip.Controllers;

// [Authorize] yazmaya gerek YOK — Program.cs'teki global filtre
// zaten tüm controller'ları koruyor.
public class GorevController : Controller
{
    // ⭐ İKİ repository: biri görev için, biri açılır liste için
    private readonly GorevRepository _gorevRepo;
    private readonly KategoriRepository _kategoriRepo;

    public GorevController(GorevRepository gorevRepo, KategoriRepository kategoriRepo)
    {
        _gorevRepo = gorevRepo;
        _kategoriRepo = kategoriRepo;
    }

    private void KategoriListesiniHazirla(long? secili = null)
    {
        var kategoriler = _kategoriRepo.TumunuGetir();

        // (kaynak liste, value alanı, görünen metin, seçili değer)
        ViewBag.Kategoriler = new SelectList(kategoriler, "KategoriId", "KategoriAd", secili);
    }

    // ════════════════════════════════════════════════════════
    //  1) LİSTELEME
    //  GET: /Gorev
    // ════════════════════════════════════════════════════════
    public IActionResult Index()
    {
        return View(_gorevRepo.TumunuGetir());
    }

    // ════════════════════════════════════════════════════════
    //  2) YENİ KAYIT FORMU
    //  GET: /Gorev/Create
    // ════════════════════════════════════════════════════════
    public IActionResult Create()
    {
        KategoriListesiniHazirla();
        return View();
    }

    // ════════════════════════════════════════════════════════
    //  3) YENİ KAYDI KAYDET
    //  POST: /Gorev/Create
    // ════════════════════════════════════════════════════════
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(Gorev gorev)
    {
        // Tarayıcı doğrulaması F12 ile kandırılabilir.
        // Bu yüzden sunucuda TEKRAR kontrol ediyoruz.
        if (!ModelState.IsValid)
        {
            // ⭐ EN ÇOK UNUTULAN SATIR
            // ViewBag sadece o istek boyunca yaşar. POST yeni bir istektir,
            // önceki ViewBag yok olmuştur. Doldurmazsak açılır liste boş
            // gelir ve sayfa NullReferenceException ile çöker.
            KategoriListesiniHazirla(gorev.KategoriId);
            return View(gorev);   // kullanıcının yazdıkları kaybolmasın
        }

        _gorevRepo.Ekle(gorev);
        TempData["Basarili"] = $"\"{gorev.Baslik}\" görevi eklendi.";

        // POST-Redirect-GET: yönlendirme yapmazsak F5'te çift kayıt olur
        return RedirectToAction("Index");
    }

    // ════════════════════════════════════════════════════════
    //  4) DÜZENLEME FORMU
    //  GET: /Gorev/Edit/5
    // ════════════════════════════════════════════════════════
    public IActionResult Edit(long id)
    {
        Gorev? gorev = _gorevRepo.IdIleGetir(id);

        // Kullanıcı adres çubuğuna /Gorev/Edit/99999 yazabilir.
        // null kontrolü ZORUNLU.
        if (gorev == null)
            return NotFound();

        KategoriListesiniHazirla(gorev.KategoriId);   // mevcut kategori seçili gelsin
        return View(gorev);
    }

    // ════════════════════════════════════════════════════════
    //  5) DÜZENLEMEYİ KAYDET
    //  POST: /Gorev/Edit/5
    // ════════════════════════════════════════════════════════
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(Gorev gorev)
    {
        if (!ModelState.IsValid)
        {
            KategoriListesiniHazirla(gorev.KategoriId);
            return View(gorev);
        }

        _gorevRepo.Guncelle(gorev);
        TempData["Basarili"] = "Görev güncellendi.";
        return RedirectToAction("Index");
    }
}
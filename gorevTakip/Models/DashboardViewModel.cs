namespace GorevTakip.Models;

public class DashboardViewModel
{
    // ── Üst kartlar ──────────────────────────────────────────
    public int ToplamGorev { get; set; }
    public int TamamlananGorev { get; set; }
    public int BekleyenGorev { get; set; }
    public int DevamEdenGorev { get; set; }
    public int GecikmisGorev { get; set; }
    public int BugunBitenGorev { get; set; }      // bugün son tarihi olanlar

    // ── Listeler ─────────────────────────────────────────────
    // = new()  →  boş başlasın, null olmasın (view'da çökmesin)
    public List<KategoriDagilim> KategoriDagilimlari { get; set; } = new();
    public List<Gorev> YaklasanGorevler { get; set; } = new();

    // ── Hesaplanan ───────────────────────────────────────────
    /// <summary>
    /// Tamamlanma yüzdesi.
    /// ⚠️ Sıfıra bölme koruması ŞART — hiç görev yoksa çökerdi.
    /// </summary>
    public int TamamlanmaYuzdesi =>
        ToplamGorev > 0
            ? (TamamlananGorev * 100) / ToplamGorev
            : 0;
}

/// <summary>
/// Kategori başına görev dağılımı.
/// Sadece dashboard'da kullanılır, bir tabloya karşılık gelmez.
/// </summary>
public class KategoriDagilim
{
    public string KategoriAd { get; set; } = "";
    public string Renk { get; set; } = "secondary";
    public int ToplamGorev { get; set; }
    public int TamamlananGorev { get; set; }

    public int Yuzde =>
        ToplamGorev > 0
            ? (TamamlananGorev * 100) / ToplamGorev
            : 0;
}
using System.ComponentModel.DataAnnotations;

namespace GorevTakip.Models;

public class Gorev
{
    public long GorevId { get; set; }

    [Required(ErrorMessage = "Kategori seçmelisiniz.")]
    [Display(Name = "Kategori")]
    public long KategoriId { get; set; }          // yabancı anahtar

    [Required(ErrorMessage = "Başlık zorunludur.")]
    [StringLength(200, ErrorMessage = "En fazla 200 karakter.")]
    [Display(Name = "Başlık")]
    public string Baslik { get; set; } = "";

    // İsteğe bağlı uzun metin
    [Display(Name = "Açıklama")]
    public string? Aciklama { get; set; }

    // 1 = Düşük, 2 = Orta, 3 = Yüksek
    [Required]
    [Range(1, 3, ErrorMessage = "Öncelik 1-3 arasında olmalı.")]
    [Display(Name = "Öncelik")]
    public int Oncelik { get; set; } = 2;

    [Required(ErrorMessage = "Durum seçmelisiniz.")]
    [Display(Name = "Durum")]
    public string Durum { get; set; } = "Beklemede";

    // ⭐ YENİ: formda BOŞ BIRAKILABİLEN tarih
    //    Sondaki ? olmasaydı, boş gönderilince "geçersiz tarih" hatası alırdık.
    [DataType(DataType.Date)]
    [Display(Name = "Bitiş tarihi")]
    public DateTime? BitisTarihi { get; set; }

    // Görev tamamlanınca dolar
    public DateTime? TamamlanmaTarihi { get; set; }

    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public bool AktifMi { get; set; } = true;

    // ── Veritabanında OLMAYAN alanlar ────────────────────────
    // JOIN ile gelir. INSERT/UPDATE'te KULLANILMAZ!
    [Display(Name = "Kategori")]
    public string KategoriAd { get; set; } = "";
    public string KategoriRenk { get; set; } = "secondary";

    // ── HESAPLANAN ÖZELLİKLER ────────────────────────────────
    // Veritabanında yok, her okunduğunda hesaplanır.

    /// <summary>
    /// Görev tamamlandı mı?
    /// </summary>
    public bool TamamlandiMi => Durum == "Tamamlandi";

    /// <summary>
    /// ⭐ Görev gecikti mi?
    /// Üç koşulun HEPSİ gerekli:
    ///   1. Bitiş tarihi var mı?          (yoksa gecikemez)
    ///   2. O tarih geçmiş mi?
    ///   3. Görev hâlâ tamamlanmamış mı?  (tamamlandıysa gecikme sayılmaz)
    /// </summary>
    public bool GecikmisMi =>
        BitisTarihi.HasValue
        && BitisTarihi.Value.Date < DateTime.Today
        && !TamamlandiMi;

    /// <summary>
    /// Bitişe kaç gün kaldı? Tarih yoksa null.
    /// Negatif değer = gecikmiş.
    /// </summary>
    public int? KalanGun =>
        BitisTarihi.HasValue
            ? (BitisTarihi.Value.Date - DateTime.Today).Days
            : null;

    /// <summary>
    /// Öncelik sayısının okunabilir karşılığı.
    /// switch ifadesi: her değere bir sonuç eşler, "_" ise varsayılan.
    /// </summary>
    public string OncelikAdi => Oncelik switch
    {
        1 => "Düşük",
        2 => "Orta",
        3 => "Yüksek",
        _ => "Bilinmiyor"
    };

    /// <summary>
    /// Önceliğin Bootstrap renk sınıfı.
    /// </summary>
    public string OncelikRenk => Oncelik switch
    {
        1 => "secondary",
        2 => "warning",
        3 => "danger",
        _ => "light"
    };
}
using System.ComponentModel.DataAnnotations;

namespace GorevTakip.Models;

public class Kategori
{
    public long KategoriId { get; set; }

    [Required(ErrorMessage = "Kategori adı zorunludur.")]
    [StringLength(100, ErrorMessage = "En fazla 100 karakter olabilir.")]
    [Display(Name = "Kategori adı")]
    public string KategoriAd { get; set; } = "";

    [Required(ErrorMessage = "Renk seçmelisiniz.")]
    [Display(Name = "Renk")]
    public string Renk { get; set; } = "primary";

    // ⭐ YENİ: NULL olabilen METİN alanı
    //    Sondaki ? → "bu değer null olabilir"
    //    [Required] YOK → boş bırakılabilir
    [StringLength(500, ErrorMessage = "En fazla 500 karakter olabilir.")]
    [Display(Name = "Açıklama")]
    public string? Aciklama { get; set; }

    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }

    // ⭐ YENİ: bool  (okul projesinde string IsActive = "1" idi)
    public bool AktifMi { get; set; } = true;

    // Veritabanında YOK — JOIN/COUNT ile gelir.
    // INSERT ve UPDATE sorgularında KULLANILMAZ!
    [Display(Name = "Görev sayısı")]
    public int GorevSayisi { get; set; }
}
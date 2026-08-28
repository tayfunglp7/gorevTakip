using System.ComponentModel.DataAnnotations;

namespace GorevTakip.Models;

public class Kullanici
{
    public long KullaniciId { get; set; }
    public string KullaniciAdi { get; set; } = "";
    public string SifreHash { get; set; } = "";   // şifrenin kendisi değil, özeti
    public string AdSoyad { get; set; } = "";
    public DateTime CreatedDate { get; set; }
    public bool AktifMi { get; set; } = true;     // ⭐ bool — BIT sütunun karşılığı
}

/// <summary>
/// Giriş formunun taşıyıcısı.
///
/// ⭐ NEDEN AYRI SINIF? — sınıfa sor:
///    Forma doğrudan Kullanici verseydik, form SifreHash alanını da
///    içerirdi. Kötü niyetli biri F12 ile gizli alan ekleyip doğrudan
///    hash göndermeyi deneyebilirdi.
///    Kural: kullanıcıdan gelen modele SADECE gereken alanları koy.
/// </summary>
public class GirisViewModel
{
    [Required(ErrorMessage = "Kullanıcı adı gerekli.")]
    [Display(Name = "Kullanıcı adı")]
    public string KullaniciAdi { get; set; } = "";

    [Required(ErrorMessage = "Şifre gerekli.")]
    [DataType(DataType.Password)]
    [Display(Name = "Şifre")]
    public string Sifre { get; set; } = "";

    [Display(Name = "Beni hatırla")]
    public bool BeniHatirla { get; set; }
}
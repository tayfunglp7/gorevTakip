using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;
using GorevTakip.Models;

namespace GorevTakip.Data;

public class KullaniciRepository
{
    private readonly string _baglantiMetni;

    public KullaniciRepository(IConfiguration configuration)
    {
        _baglantiMetni = configuration.GetConnectionString("GorevDb")!;
    }

    /// <summary>
    /// Metnin SHA256 özetini döner.
    /// static → nesne oluşturmadan çağrılabilir:
    ///     KullaniciRepository.SifreyiHashle("gorev123")
    /// </summary>
    public static string SifreyiHashle(string sifre)
    {
        byte[] bayt = Encoding.UTF8.GetBytes(sifre);   // metni bayta çevir
        byte[] hash = SHA256.HashData(bayt);           // özeti hesapla
        return Convert.ToHexString(hash);              // okunabilir metne çevir
    }

    /// <summary>
    /// Kullanıcı adı ve şifre doğruysa kullanıcıyı, yanlışsa null döner.
    ///
    /// ⭐ Karşılaştırmayı VERİTABANINDA yapıyoruz:
    ///    "bu ad VE bu hash'e sahip satır var mı?"
    ///    Böylece hash hiç uygulamaya taşınmıyor.
    /// </summary>
    public Kullanici? Dogrula(string kullaniciAdi, string sifre)
    {
        string hash = SifreyiHashle(sifre);

        // sifre_hash sütununu SELECT'e koymuyoruz — dolaşmasına gerek yok
        string sql = @"SELECT kullanici_id, kullanici_adi, ad_soyad,
                              created_date, aktif_mi
                       FROM kullanici
                       WHERE kullanici_adi = @ad
                         AND sifre_hash    = @hash
                         AND aktif_mi      = 1";

        using (SqlConnection baglanti = new SqlConnection(_baglantiMetni))
        using (SqlCommand komut = new SqlCommand(sql, baglanti))
        {
            // ⭐ Giriş formu, SQL injection'ın en klasik hedefidir.
            //    Şifre alanına  ' OR '1'='1  yazan biri,
            //    parametre kullanmasaydık içeri girerdi.
            komut.Parameters.AddWithValue("@ad", kullaniciAdi);
            komut.Parameters.AddWithValue("@hash", hash);

            baglanti.Open();

            using (SqlDataReader okuyucu = komut.ExecuteReader())
            {
                if (okuyucu.Read())
                {
                    return new Kullanici
                    {
                        KullaniciId = okuyucu.GetInt64(okuyucu.GetOrdinal("kullanici_id")),
                        KullaniciAdi = okuyucu.GetString(okuyucu.GetOrdinal("kullanici_adi")),
                        AdSoyad = okuyucu.GetString(okuyucu.GetOrdinal("ad_soyad")),
                        CreatedDate = okuyucu.GetDateTime(okuyucu.GetOrdinal("created_date")),

                        // ⭐ BIT sütunu → GetBoolean
                        AktifMi = okuyucu.GetBoolean(okuyucu.GetOrdinal("aktif_mi"))
                    };
                }
            }
        }

        return null;   // kullanıcı yok VEYA şifre yanlış
    }
}
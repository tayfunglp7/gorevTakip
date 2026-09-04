using Microsoft.Data.SqlClient;
using GorevTakip.Models;

namespace GorevTakip.Data;

public class DashboardRepository
{
    private readonly string _baglantiMetni;

    public DashboardRepository(IConfiguration configuration)
    {
        _baglantiMetni = configuration.GetConnectionString("GorevDb")!;
    }

    /// <summary>
    /// Altı sayıyı TEK sorguda getirir.
    /// out parametreleriyle birden çok değer dışarı verilir.
    /// </summary>
    public void SayilariGetir(out int toplam, out int tamamlanan, out int bekleyen,
                              out int devamEden, out int gecikmis, out int bugunBiten)
    {
        string sql = @"
            SELECT
                COUNT(*) AS toplam,

                SUM(CASE WHEN durum = 'Tamamlandi'   THEN 1 ELSE 0 END) AS tamamlanan,
                SUM(CASE WHEN durum = 'Beklemede'    THEN 1 ELSE 0 END) AS bekleyen,
                SUM(CASE WHEN durum = 'Devam ediyor' THEN 1 ELSE 0 END) AS devam_eden,

                -- Gecikmiş: tarihi var + geçmiş + tamamlanmamış
                SUM(CASE WHEN bitis_tarihi IS NOT NULL
                          AND bitis_tarihi < CAST(GETDATE() AS DATE)
                          AND durum <> 'Tamamlandi'
                         THEN 1 ELSE 0 END) AS gecikmis,

                -- Bugün bitmesi gerekenler
                SUM(CASE WHEN bitis_tarihi = CAST(GETDATE() AS DATE)
                          AND durum <> 'Tamamlandi'
                         THEN 1 ELSE 0 END) AS bugun_biten

            FROM gorev
            WHERE aktif_mi = 1";

        using (SqlConnection baglanti = new SqlConnection(_baglantiMetni))
        using (SqlCommand komut = new SqlCommand(sql, baglanti))
        {
            baglanti.Open();

            using (SqlDataReader okuyucu = komut.ExecuteReader())
            {
                if (okuyucu.Read())
                {
                    toplam = okuyucu.GetInt32(okuyucu.GetOrdinal("toplam"));

                    // ⚠️ SUM boş tabloda NULL döner, 0 değil!
                    //    Hiç görev yoksa uygulama çökerdi.
                    tamamlanan = OkuVeyaSifir(okuyucu, "tamamlanan");
                    bekleyen = OkuVeyaSifir(okuyucu, "bekleyen");
                    devamEden = OkuVeyaSifir(okuyucu, "devam_eden");
                    gecikmis = OkuVeyaSifir(okuyucu, "gecikmis");
                    bugunBiten = OkuVeyaSifir(okuyucu, "bugun_biten");
                }
                else
                {
                    toplam = tamamlanan = bekleyen = devamEden = gecikmis = bugunBiten = 0;
                }
            }
        }
    }

    /// <summary>
    /// Sütun NULL ise 0 döner. SUM'ın boş tabloda NULL dönmesine karşı koruma.
    /// </summary>
    private int OkuVeyaSifir(SqlDataReader okuyucu, string sutunAdi)
    {
        int sutunNo = okuyucu.GetOrdinal(sutunAdi);
        return okuyucu.IsDBNull(sutunNo) ? 0 : okuyucu.GetInt32(sutunNo);
    }

    /// <summary>
    /// Kategorilere göre görev dağılımı.
    /// </summary>
    public List<KategoriDagilim> KategoriDagilimi()
    {
        var liste = new List<KategoriDagilim>();

        string sql = @"
            SELECT k.kategori_ad,
                   k.renk,
                   COUNT(g.gorev_id) AS toplam,
                   SUM(CASE WHEN g.durum = 'Tamamlandi' THEN 1 ELSE 0 END) AS tamamlanan
            FROM kategori k
            LEFT JOIN gorev g ON g.kategori_id = k.kategori_id AND g.aktif_mi = 1
            WHERE k.aktif_mi = 1
            GROUP BY k.kategori_ad, k.renk
            ORDER BY toplam DESC";

        using (SqlConnection baglanti = new SqlConnection(_baglantiMetni))
        using (SqlCommand komut = new SqlCommand(sql, baglanti))
        {
            baglanti.Open();

            using (SqlDataReader okuyucu = komut.ExecuteReader())
            {
                while (okuyucu.Read())
                {
                    liste.Add(new KategoriDagilim
                    {
                        KategoriAd = okuyucu.GetString(okuyucu.GetOrdinal("kategori_ad")),
                        Renk = okuyucu.GetString(okuyucu.GetOrdinal("renk")),
                        ToplamGorev = okuyucu.GetInt32(okuyucu.GetOrdinal("toplam")),
                        TamamlananGorev = OkuVeyaSifir(okuyucu, "tamamlanan")
                    });
                }
            }
        }

        return liste;
    }

    /// <summary>
    /// Yaklaşan görevler: bitiş tarihi olan, tamamlanmamış, en yakın N tanesi.
    /// </summary>
    public List<Gorev> YaklasanGorevler(int adet = 5)
    {
        var liste = new List<Gorev>();

        // ⭐ TOP (@adet) — parametre kullanınca PARANTEZ şart
        string sql = @"SELECT TOP (@adet)
                              g.gorev_id, g.baslik, g.oncelik, g.durum,
                              g.bitis_tarihi, k.kategori_ad, k.renk
                       FROM gorev g
                       INNER JOIN kategori k ON g.kategori_id = k.kategori_id
                       WHERE g.aktif_mi = 1
                         AND g.durum <> 'Tamamlandi'
                         AND g.bitis_tarihi IS NOT NULL
                       ORDER BY g.bitis_tarihi";

        using (SqlConnection baglanti = new SqlConnection(_baglantiMetni))
        using (SqlCommand komut = new SqlCommand(sql, baglanti))
        {
            komut.Parameters.AddWithValue("@adet", adet);
            baglanti.Open();

            using (SqlDataReader okuyucu = komut.ExecuteReader())
            {
                while (okuyucu.Read())
                {
                    // NOT: Gorev nesnesinin TÜM alanlarını doldurmuyoruz —
                    // bu ekranda sadece birkaçını göstereceğiz.
                    // SELECT'te olmayan sütunu okumaya kalkarsan hata alırsın.
                    var g = new Gorev
                    {
                        GorevId = okuyucu.GetInt64(okuyucu.GetOrdinal("gorev_id")),
                        Baslik = okuyucu.GetString(okuyucu.GetOrdinal("baslik")),
                        Oncelik = okuyucu.GetInt32(okuyucu.GetOrdinal("oncelik")),
                        Durum = okuyucu.GetString(okuyucu.GetOrdinal("durum")),
                        BitisTarihi = okuyucu.GetDateTime(okuyucu.GetOrdinal("bitis_tarihi")),
                        KategoriAd = okuyucu.GetString(okuyucu.GetOrdinal("kategori_ad")),
                        KategoriRenk = okuyucu.GetString(okuyucu.GetOrdinal("renk"))
                    };

                    liste.Add(g);
                }
            }
        }

        return liste;
    }
}
using Microsoft.Data.SqlClient;
using GorevTakip.Models;

namespace GorevTakip.Data;

public class GorevRepository
{
    private readonly string _baglantiMetni;

    public GorevRepository(IConfiguration configuration)
    {
        _baglantiMetni = configuration.GetConnectionString("GorevDb")!;
    }

    private Gorev SatiriNesneyeCevir(SqlDataReader okuyucu)
    {
        Gorev g = new Gorev();

        g.GorevId = okuyucu.GetInt64(okuyucu.GetOrdinal("gorev_id"));
        g.KategoriId = okuyucu.GetInt64(okuyucu.GetOrdinal("kategori_id"));
        g.Baslik = okuyucu.GetString(okuyucu.GetOrdinal("baslik"));
        g.Oncelik = okuyucu.GetInt32(okuyucu.GetOrdinal("oncelik"));
        g.Durum = okuyucu.GetString(okuyucu.GetOrdinal("durum"));
        g.CreatedDate = okuyucu.GetDateTime(okuyucu.GetOrdinal("created_date"));

        // BIT sütunu → GetBoolean
        g.AktifMi = okuyucu.GetBoolean(okuyucu.GetOrdinal("aktif_mi"));

        // ── NULL olabilen dört sütun ─────────────────────────
        int aciklamaSutun = okuyucu.GetOrdinal("aciklama");
        g.Aciklama = okuyucu.IsDBNull(aciklamaSutun)
            ? null
            : okuyucu.GetString(aciklamaSutun);

        int bitisSutun = okuyucu.GetOrdinal("bitis_tarihi");
        g.BitisTarihi = okuyucu.IsDBNull(bitisSutun)
            ? null
            : okuyucu.GetDateTime(bitisSutun);

        int tamamlanmaSutun = okuyucu.GetOrdinal("tamamlanma_tarihi");
        g.TamamlanmaTarihi = okuyucu.IsDBNull(tamamlanmaSutun)
            ? null
            : okuyucu.GetDateTime(tamamlanmaSutun);

        int guncellemeSutun = okuyucu.GetOrdinal("updated_date");
        g.UpdatedDate = okuyucu.IsDBNull(guncellemeSutun)
            ? null
            : okuyucu.GetDateTime(guncellemeSutun);

        return g;
    }

    // ════════════════════════════════════════════════════════
    //  1) LİSTELE
    // ════════════════════════════════════════════════════════
    public List<Gorev> TumunuGetir()
    {
        List<Gorev> liste = new List<Gorev>();

        string sql = @"SELECT g.gorev_id, g.kategori_id, g.baslik, g.aciklama,
                              g.oncelik, g.durum, g.bitis_tarihi, g.tamamlanma_tarihi,
                              g.created_date, g.updated_date, g.aktif_mi,
                              k.kategori_ad, k.renk
                       FROM gorev g
                       INNER JOIN kategori k ON g.kategori_id = k.kategori_id
                       WHERE g.aktif_mi = 1
                       ORDER BY
                           -- ⭐ KOŞULLU SIRALAMA (aşağıda açıklanıyor)
                           CASE WHEN g.durum = 'Tamamlandi' THEN 1 ELSE 0 END,
                           g.oncelik DESC,
                           CASE WHEN g.bitis_tarihi IS NULL THEN 1 ELSE 0 END,
                           g.bitis_tarihi";

        using (SqlConnection baglanti = new SqlConnection(_baglantiMetni))
        using (SqlCommand komut = new SqlCommand(sql, baglanti))
        {
            baglanti.Open();

            using (SqlDataReader okuyucu = komut.ExecuteReader())
            {
                while (okuyucu.Read())
                {
                    Gorev g = SatiriNesneyeCevir(okuyucu);

                    // JOIN'den gelen ekstra sütunlar
                    g.KategoriAd = okuyucu.GetString(okuyucu.GetOrdinal("kategori_ad"));
                    g.KategoriRenk = okuyucu.GetString(okuyucu.GetOrdinal("renk"));

                    liste.Add(g);
                }
            }
        }

        return liste;
    }

    // ════════════════════════════════════════════════════════
    //  2) READ — tek görev
    // ════════════════════════════════════════════════════════
    public Gorev? IdIleGetir(long id)
    {
        Gorev? sonuc = null;

        string sql = @"SELECT g.gorev_id, g.kategori_id, g.baslik, g.aciklama,
                              g.oncelik, g.durum, g.bitis_tarihi, g.tamamlanma_tarihi,
                              g.created_date, g.updated_date, g.aktif_mi,
                              k.kategori_ad, k.renk
                       FROM gorev g
                       INNER JOIN kategori k ON g.kategori_id = k.kategori_id
                       WHERE g.gorev_id = @id";

        using (SqlConnection baglanti = new SqlConnection(_baglantiMetni))
        using (SqlCommand komut = new SqlCommand(sql, baglanti))
        {
            komut.Parameters.AddWithValue("@id", id);
            baglanti.Open();

            using (SqlDataReader okuyucu = komut.ExecuteReader())
            {
                // while değil if — tek satır bekliyoruz
                if (okuyucu.Read())
                {
                    sonuc = SatiriNesneyeCevir(okuyucu);
                    sonuc.KategoriAd = okuyucu.GetString(okuyucu.GetOrdinal("kategori_ad"));
                    sonuc.KategoriRenk = okuyucu.GetString(okuyucu.GetOrdinal("renk"));
                }
            }
        }

        return sonuc;   // bulunamazsa null — controller kontrol etmeli
    }

    // ════════════════════════════════════════════════════════
    //  3) CREATE
    // ════════════════════════════════════════════════════════
    public void Ekle(Gorev gorev)
    {
        // gorev_id yazılmaz (IDENTITY)
        // kategori_ad / renk hiç yazılmaz (bu tabloda yoklar)
        string sql = @"INSERT INTO gorev
                          (kategori_id, baslik, aciklama, oncelik, durum,
                           bitis_tarihi, tamamlanma_tarihi,
                           created_date, updated_date, aktif_mi)
                       VALUES
                          (@kategoriId, @baslik, @aciklama, @oncelik, @durum,
                           @bitisTarihi, @tamamlanmaTarihi,
                           @createdDate, NULL, 1)";

        using (SqlConnection baglanti = new SqlConnection(_baglantiMetni))
        using (SqlCommand komut = new SqlCommand(sql, baglanti))
        {
            komut.Parameters.AddWithValue("@kategoriId", gorev.KategoriId);
            komut.Parameters.AddWithValue("@baslik", gorev.Baslik);
            komut.Parameters.AddWithValue("@oncelik", gorev.Oncelik);
            komut.Parameters.AddWithValue("@durum", gorev.Durum);

            // ⭐ NULL olabilen alanlar — DBNull.Value köprüsü
            komut.Parameters.AddWithValue("@aciklama",
                (object?)gorev.Aciklama ?? DBNull.Value);

            komut.Parameters.AddWithValue("@bitisTarihi",
                (object?)gorev.BitisTarihi ?? DBNull.Value);

            // Görev "Tamamlandi" olarak eklendiyse tamamlanma tarihi de yazılmalı
            if (gorev.Durum == "Tamamlandi")
                komut.Parameters.AddWithValue("@tamamlanmaTarihi", DateTime.Now);
            else
                komut.Parameters.AddWithValue("@tamamlanmaTarihi", DBNull.Value);

            komut.Parameters.AddWithValue("@createdDate", DateTime.Now);

            baglanti.Open();
            komut.ExecuteNonQuery();
        }
    }

    // ════════════════════════════════════════════════════════
    //  4) UPDATE
    // ════════════════════════════════════════════════════════
    public void Guncelle(Gorev gorev)
    {
        // ⚠️ WHERE'i unutursan TÜM görevler aynı kayda dönüşür
        string sql = @"UPDATE gorev
                       SET kategori_id       = @kategoriId,
                           baslik            = @baslik,
                           aciklama          = @aciklama,
                           oncelik           = @oncelik,
                           durum             = @durum,
                           bitis_tarihi      = @bitisTarihi,
                           tamamlanma_tarihi = @tamamlanmaTarihi,
                           updated_date      = @updatedDate
                       WHERE gorev_id        = @id";

        using (SqlConnection baglanti = new SqlConnection(_baglantiMetni))
        using (SqlCommand komut = new SqlCommand(sql, baglanti))
        {
            komut.Parameters.AddWithValue("@kategoriId", gorev.KategoriId);
            komut.Parameters.AddWithValue("@baslik", gorev.Baslik);
            komut.Parameters.AddWithValue("@oncelik", gorev.Oncelik);
            komut.Parameters.AddWithValue("@durum", gorev.Durum);

            komut.Parameters.AddWithValue("@aciklama",
                (object?)gorev.Aciklama ?? DBNull.Value);

            komut.Parameters.AddWithValue("@bitisTarihi",
                (object?)gorev.BitisTarihi ?? DBNull.Value);

            // ⭐ Durum ile tamamlanma tarihini BİRLİKTE yönetiyoruz.
            //    Kullanıcı formdan "Tamamlandi" seçtiyse tarih yazılır;
            //    geri aldıysa tarih silinir. İkisi hep tutarlı kalır.
            if (gorev.Durum == "Tamamlandi")
            {
                // Zaten tamamlanmışsa eski tarihi koru, yeni tamamlandıysa şimdi yaz
                komut.Parameters.AddWithValue("@tamamlanmaTarihi",
                    (object?)gorev.TamamlanmaTarihi ?? DateTime.Now);
            }
            else
            {
                komut.Parameters.AddWithValue("@tamamlanmaTarihi", DBNull.Value);
            }

            komut.Parameters.AddWithValue("@updatedDate", DateTime.Now);
            komut.Parameters.AddWithValue("@id", gorev.GorevId);

            baglanti.Open();
            komut.ExecuteNonQuery();
        }

        // created_date'e dokunmuyoruz — kayıt tarihi değişmemeli
    }
}

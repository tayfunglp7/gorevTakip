using Microsoft.Data.SqlClient;
using GorevTakip.Models;

namespace GorevTakip.Data;

public class KategoriRepository
{
    private readonly string _baglantiMetni;

    public KategoriRepository(IConfiguration configuration)
    {
        _baglantiMetni = configuration.GetConnectionString("GorevDb")!;
    }

    // ════════════════════════════════════════════════════════
    //  YARDIMCI: satırı nesneye çevir
    // ════════════════════════════════════════════════════════
    private Kategori SatiriNesneyeCevir(SqlDataReader okuyucu)
    {
        Kategori k = new Kategori();

        k.KategoriId = okuyucu.GetInt64(okuyucu.GetOrdinal("kategori_id"));
        k.KategoriAd = okuyucu.GetString(okuyucu.GetOrdinal("kategori_ad"));
        k.Renk = okuyucu.GetString(okuyucu.GetOrdinal("renk"));
        k.CreatedDate = okuyucu.GetDateTime(okuyucu.GetOrdinal("created_date"));

        // ⭐ BIT sütunu → GetBoolean
        k.AktifMi = okuyucu.GetBoolean(okuyucu.GetOrdinal("aktif_mi"));

        // ⭐ NULL olabilen METİN — kontrolsüz okursak uygulama çöker
        int aciklamaSutun = okuyucu.GetOrdinal("aciklama");
        k.Aciklama = okuyucu.IsDBNull(aciklamaSutun)
            ? null
            : okuyucu.GetString(aciklamaSutun);

        // NULL olabilen TARİH — aynı mantık
        int guncellemeSutun = okuyucu.GetOrdinal("updated_date");
        k.UpdatedDate = okuyucu.IsDBNull(guncellemeSutun)
            ? null
            : okuyucu.GetDateTime(guncellemeSutun);

        return k;
    }

    // ════════════════════════════════════════════════════════
    //  1) READ — tüm aktif kategoriler + görev sayıları
    // ════════════════════════════════════════════════════════
    public List<Kategori> TumunuGetir()
    {
        List<Kategori> liste = new List<Kategori>();

        // Alt sorgu: her kategori için o kategorideki görevleri say
        string sql = @"SELECT k.kategori_id, k.kategori_ad, k.renk, k.aciklama,
                              k.created_date, k.updated_date, k.aktif_mi,
                              (SELECT COUNT(*) FROM gorev g
                               WHERE g.kategori_id = k.kategori_id
                                 AND g.aktif_mi = 1) AS gorev_sayisi
                       FROM kategori k
                       WHERE k.aktif_mi = 1
                       ORDER BY k.kategori_ad";

        using (SqlConnection baglanti = new SqlConnection(_baglantiMetni))
        using (SqlCommand komut = new SqlCommand(sql, baglanti))
        {
            baglanti.Open();

            using (SqlDataReader okuyucu = komut.ExecuteReader())
            {
                while (okuyucu.Read())
                {
                    Kategori k = SatiriNesneyeCevir(okuyucu);
                    k.GorevSayisi = okuyucu.GetInt32(okuyucu.GetOrdinal("gorev_sayisi"));
                    liste.Add(k);
                }
            }
        }

        return liste;
    }

    // ════════════════════════════════════════════════════════
    //  2) READ — tek kategori
    // ════════════════════════════════════════════════════════
    public Kategori? IdIleGetir(long id)
    {
        Kategori? sonuc = null;

        string sql = @"SELECT kategori_id, kategori_ad, renk, aciklama,
                              created_date, updated_date, aktif_mi
                       FROM kategori
                       WHERE kategori_id = @id";

        using (SqlConnection baglanti = new SqlConnection(_baglantiMetni))
        using (SqlCommand komut = new SqlCommand(sql, baglanti))
        {
            komut.Parameters.AddWithValue("@id", id);
            baglanti.Open();

            using (SqlDataReader okuyucu = komut.ExecuteReader())
            {
                if (okuyucu.Read())
                    sonuc = SatiriNesneyeCevir(okuyucu);
            }
        }

        return sonuc;
    }

    // ════════════════════════════════════════════════════════
    //  3) CREATE
    // ════════════════════════════════════════════════════════
    public void Ekle(Kategori kategori)
    {
        string sql = @"INSERT INTO kategori
                          (kategori_ad, renk, aciklama, created_date, updated_date, aktif_mi)
                       VALUES
                          (@ad, @renk, @aciklama, @createdDate, NULL, 1)";

        using (SqlConnection baglanti = new SqlConnection(_baglantiMetni))
        using (SqlCommand komut = new SqlCommand(sql, baglanti))
        {
            komut.Parameters.AddWithValue("@ad", kategori.KategoriAd);
            komut.Parameters.AddWithValue("@renk", kategori.Renk);

            // ⭐ NULL DEĞER GÖNDERME — çok önemli!
            //    AddWithValue(..., null) yazarsan çalışmaz.
            //    C#'ın null'ı ile SQL'in NULL'ı farklı şeylerdir;
            //    aradaki köprü DBNull.Value'dur.
            komut.Parameters.AddWithValue("@aciklama",
                (object?)kategori.Aciklama ?? DBNull.Value);

            komut.Parameters.AddWithValue("@createdDate", DateTime.Now);

            baglanti.Open();
            komut.ExecuteNonQuery();
        }
    }

    // ════════════════════════════════════════════════════════
    //  4) UPDATE
    // ════════════════════════════════════════════════════════
    public void Guncelle(Kategori kategori)
    {
        // ⚠️ WHERE'i unutursan TÜM kategoriler aynı isme dönüşür
        string sql = @"UPDATE kategori
                       SET kategori_ad  = @ad,
                           renk         = @renk,
                           aciklama     = @aciklama,
                           updated_date = @updatedDate
                       WHERE kategori_id = @id";

        using (SqlConnection baglanti = new SqlConnection(_baglantiMetni))
        using (SqlCommand komut = new SqlCommand(sql, baglanti))
        {
            komut.Parameters.AddWithValue("@ad", kategori.KategoriAd);
            komut.Parameters.AddWithValue("@renk", kategori.Renk);
            komut.Parameters.AddWithValue("@aciklama",
                (object?)kategori.Aciklama ?? DBNull.Value);
            komut.Parameters.AddWithValue("@updatedDate", DateTime.Now);
            komut.Parameters.AddWithValue("@id", kategori.KategoriId);

            baglanti.Open();
            komut.ExecuteNonQuery();
        }
    }

    // ════════════════════════════════════════════════════════
    //  5) DELETE — soft delete
    // ════════════════════════════════════════════════════════
    public void PasifYap(long id)
    {
        string sql = @"UPDATE kategori
                       SET aktif_mi = 0, updated_date = @updatedDate
                       WHERE kategori_id = @id";

        using (SqlConnection baglanti = new SqlConnection(_baglantiMetni))
        using (SqlCommand komut = new SqlCommand(sql, baglanti))
        {
            komut.Parameters.AddWithValue("@updatedDate", DateTime.Now);
            komut.Parameters.AddWithValue("@id", id);

            baglanti.Open();
            komut.ExecuteNonQuery();
        }
    }

    // ════════════════════════════════════════════════════════
    //  Bu kategoride kaç aktif görev var?
    //  (Silme kontrolü için — Modül 5'te kullanacağız)
    // ════════════════════════════════════════════════════════
    public int AktifGorevSayisi(long kategoriId)
    {
        string sql = @"SELECT COUNT(*) FROM gorev
                       WHERE kategori_id = @id AND aktif_mi = 1";

        using (SqlConnection baglanti = new SqlConnection(_baglantiMetni))
        using (SqlCommand komut = new SqlCommand(sql, baglanti))
        {
            komut.Parameters.AddWithValue("@id", kategoriId);
            baglanti.Open();
            return Convert.ToInt32(komut.ExecuteScalar());
        }
    }
}
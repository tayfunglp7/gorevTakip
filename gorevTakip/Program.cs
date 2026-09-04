using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using GorevTakip.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// ── 1. "Giriş zorunlu" filtresi ─────────────────────────────
//
// ⭐ Neden global filtre, her controller'a [Authorize] değil?
//    Yarın yeni controller yazıp [Authorize] koymayı unutursan
//    o sayfa herkese açık kalır. Global filtreyle varsayılan KAPALI olur.
//    GÜVENLİK İLKESİ: varsayılan hep en kısıtlayıcı seçenek olmalı.
builder.Services.AddControllersWithViews(secenekler =>
{
    var politika = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    secenekler.Filters.Add(new AuthorizeFilter(politika));
});

// ── 2. Çerez ayarı ──────────────────────────────────────────
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(secenekler =>
    {
        secenekler.LoginPath = "/Hesap/Giris";
        secenekler.ReturnUrlParameter = "donusUrl";   // controller parametresiyle aynı olmalı!
        secenekler.ExpireTimeSpan = TimeSpan.FromHours(8);
        secenekler.SlidingExpiration = true;
        secenekler.Cookie.HttpOnly = true;            // JS çereze erişemez → XSS koruması
        secenekler.Cookie.SameSite = SameSiteMode.Lax; // CSRF koruması
        secenekler.Cookie.Name = "GorevTakip.Oturum";
    });


builder.Services.AddScoped<KullaniciRepository>();
builder.Services.AddScoped<KategoriRepository>();
builder.Services.AddScoped<GorevRepository>();
builder.Services.AddScoped<DashboardRepository>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

// ⭐⭐ SIRA KRİTİK
app.UseAuthentication();   // ÖNCE: "sen kimsin?" (çerezi okur)
app.UseAuthorization();    // SONRA: "girebilir mi?" (filtreyi uygular)

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Hesap}/{action=Giris}/{id?}")
    .WithStaticAssets();


app.Run();

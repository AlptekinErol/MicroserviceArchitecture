# Commit Message Guide (Türkçe + Emojili + Conventional)

## Biçim
- **Konu satırı (satır 1):** `<emoji> <type>(<scope>): <kısa özet>` (maks. 72 karakter)
- **Gövde (isteğe bağlı):** Neden/Ne/Etki (madde madde olabilir)
- **Footer (isteğe bağlı):** İlgili issue/PR, breaking change notu

## Emojiler (tek tane seç)
- ✨ `feat` – Yeni özellik
- 🐛 `fix` – Hata düzeltme
- 🧹 `chore` – Derleme/araç/güncelleme
- 📝 `docs` – Dokümantasyon
- 🎨 `style` – Stil/format (iş mantığı yok)
- ♻️ `refactor` – Refactor (işlev değişmeden)
- ⚡ `perf` – Performans
- ✅ `test` – Testler
- 🚚 `build` – Build/CI/CD
- 🔒 `security` – Güvenlik
- 🔁 `revert` – Geri alma
- ⚠️ `breaking` – Uyum bozucu değişiklik

## Type (Conventional Commits)
`feat`, `fix`, `docs`, `style`, `refactor`, `perf`, `test`, `chore`, `build`, `ci`, `revert`

## Scope
- Dosya/dizin ya da modül adı: `api`, `search`, `auth`, `infra`, `ui`, `db` vb.
- Kapsamı değişikliklerden **tahmin et** (ör. en çok değişen klasör).

## Dil ve Ton
- **Varsayılan dil Türkçe.** (İngilizce ihtiyaç varsa üret, ama kısa ve net.)
- Emir kipinde, şimdiki zaman: “ekle”, “düzelt”, “iyileştir”.
- Kısa, öz, bilgi yoğun.

## Gövde (isteğe bağlı)
- **Neden**: Bu değişiklik niye yapıldı?
- **Ne**: Ne değişti? (madde madde)
- **Etki**: Performans, API sözleşmesi, mig. notu vb.

## Footer (isteğe bağlı)
- `Refs: #123`, `Closes: #456`
- **BREAKING CHANGE:** açıkça belirt.

## Örnekler
- `✨ feat(api): arama endpointine filtreleme eklendi`
- `🐛 fix(auth): token yenilemede null referans hatası giderildi`
- `♻️ refactor(search): sorgu derleyici sadeleştirildi`
- `⚡ perf(db): indeks eklendi; sorgu süresi %35 iyileşti`
- `⚠️ breaking feat(ui): tablo kolon isimleri güncellendi`

## Talimatlar (Copilot’a)
1. Staged diff’i analiz et ve **tek bir ana amaç** seç: `feat`/`fix`/`refactor` vb.
2. Uygun **emojiyi** yukarıdaki tablodan seç ve **sadece bir emoji** kullan.
3. **Scope’u** en çok etkilenen paket/dizin adına göre otomatik belirle.
4. **Özet satırını 72 karakteri geçirme**; gereksiz süsleme yapma.
5. Eğer anlamlıysa gövdeyi 2–5 madde ile doldur (Neden/Ne/Etki).
6. Issue/PR numarası görürsen `Refs:`/`Closes:` ile **Footer** ekle.
7. API sözleşmesi kırılıyorsa `⚠️ breaking` ve **BREAKING CHANGE** notu ekle.

# Copilot Instructions
oy

## Proje Yönergeleri
- Uygulamanın adı artık 'Rechnung' olarak kullanılmalı; 'Örnek' adı yeni değişikliklerde kullanılmamalı.
- Arayüzde Türkçe metin kullanma; tüm görünen alanları Almanca yap.
- Kullanıcı, sağ alanda ayrı bir canlı PDF önizlemesi değil; PDF ile aynı görünüme sahip, doğrudan düzenlenebilir bir çalışma alanı istiyor. Düzenleme bu alanda yapılmalı ve üretilen PDF aynı düzeni kullanmalı.
- Pozisyon düzenleme alanında başlıkların ve satır içeriklerinin ortalı olmasını sağla; optik olarak daha zarif ve kibar bir görünüm koru.
- Ana sayfada sol boşluk, orta boşluk ve sağ boşluğun optik olarak simetrik olmasını sağla.
- Sol yan menüyü tek kolon yap; menü öğelerini eşit genişlikte düzenle; ikonları sola sabitle; tüm öğeler aynı dikey hizada, tutarlı aralıklı ve profesyonel görünsün.
- Büyük değişikliklerden önce mevcut duruma geri dönülebilecek bir geri dönüş noktası oluştur.
- Bu anı geri dönüş noktası olarak kaydet; kullanıcı ileride "kaldır" dediğinde değişiklikleri tam olarak bu mevcut duruma geri al.
- Kullanıcı, işlemleri durmadan ve onay beklemeden sürdürmemi istiyor; bu yüzden işlemler otomatik olarak ilerlemelidir.
- Finans ekranında 'kazanılan vergi' ifadesi yerine 'ödenmesi gereken vergi' ifadesini kullan; kartlar optik olarak hizalı olmalı ve küçük pencerede sığmalı.
- Kullanıcı dağıtım ve kurulum tarafında profesyonel görünen, mümkün olduğunca tamamlanmış bir setup akışı istiyor.

### Tarama ve Yazıcı/Tarayıcı Entegrasyonu
- Uygulamaya WLAN üzerinden bağlı yazıcı/tarayıcılarla uyumlu gelişmiş bir tarama özelliği ekle.
- Tarama iş akışını tarama → arşivleme → (mümkünse) OCR şeklinde tasarla; OCR opsiyonel ama mevcutsa aranabilir ve düzenlenebilir metin üret.
- Giriş faturası ve tarama belgelerini, ilgili seçilmiş yazılı faturaya doğrudan bağlama (attach) yöntemini varsayılan ve tercih edilen ilişkilendirme yöntemi olarak uygula; kullanıcıya ilişkilendirmeyi doğrulama veya elle değiştirme seçeneği sun.
- Tarama sonuçlarını doğrudan düzenlenebilir çalışma alanına gönder; üretilen PDF mevcut düzeni korumalı ve kullanıcı aynı ortamda düzenlemeye devam edebilmeli.
- Tarama sırasında cihaz seçimi, hedef klasör, dosya adlandırma şablonları ve temel meta veri (ör. tarih, etiket, kaynak cihaz) seçenekleri sun.
- Ağ bağlantı sorunlarına karşı hata durumlarını, yeniden deneme ve kullanıcıya yönlendirici açıklamalar gösterme mekanizmalarını ekle.
- Tarama ve arşivleme işlemleri için performans ve güvenlik (şifreli transfer, erişim yetkilendirme) gereksinimlerini göz önünde tut.

## Kaydetme Noktaları
- Büyük değişikliklerden önce mevcut durumu anı (checkpoint) olarak kaydet; kullanıcı beğenmezse tam olarak bu duruma geri dönebilme imkanı sağla.
## Bellek
- Kullanıcı, uygun geliştirme görevlerinde onay istemeden ve araya soru koymadan tüm adımları uygulamamı istiyor.
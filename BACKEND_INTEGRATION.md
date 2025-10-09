# Backend API Entegrasyonu - Favoriler

Bu dokümantasyon, favori sistemi için backend API entegrasyonunun nasıl aktifleştirileceğini açıklar.

## Mevcut Durum

- ✅ Frontend favori sistemi hazır
- ✅ localStorage fallback mevcut
- ✅ Backend controller ve servis kodları hazır
- ❌ Backend API henüz aktif değil
- ❌ Auth sistemi henüz aktif değil

## Backend API Aktifleştirme Adımları

### 1. API Config Güncellemesi

`src/config/apiConfig.js` dosyasında:

```javascript
export const API_CONFIG = {
  BACKEND_ENABLED: true, // false → true yap
  AUTH_ENABLED: true, // false → true yap (auth hazır olduğunda)
  USE_MOCK_DATA: false, // true → false yap
};
```

### 2. Auth Sistemi Entegrasyonu

Kullanıcı giriş yapıldığında localStorage'a kaydet:

```javascript
// Login başarılı olduğunda
const loginUser = (userData) => {
  localStorage.setItem(
    "authUser",
    JSON.stringify({
      id: userData.userId,
      name: userData.name,
      email: userData.email,
    })
  );
};

// Logout olduğunda
const logoutUser = () => {
  localStorage.removeItem("authUser");
};
```

### 3. Backend API Endpoints

Aşağıdaki endpoint'lerin çalışır olması gerekir:

- `GET /api/favorites?userId={guid}` - Kullanıcının favorilerini getir
- `POST /api/favorites/{productId}?userId={guid}` - Favoriye ekle/çıkar
- `DELETE /api/favorites/{productId}?userId={guid}` - Favoriden sil

### 4. Test Edilecek Durumlar

#### Frontend Test Senaryoları:

1. **Giriş yapmamış kullanıcı:**

   - Favoriler localStorage'dan çekilmeli
   - Favori ekleme localStorage'a kaydedilmeli

2. **Giriş yapmış kullanıcı (API aktif):**

   - Favoriler backend'den çekilmeli
   - Favori ekleme backend'e gönderilmeli
   - API hatası durumunda localStorage fallback çalışmalı

3. **Giriş yapmış kullanıcı (API pasif):**
   - localStorage fallback çalışmalı
   - Hata mesajları console'da görünmeli

## Kod Yapısı

### Frontend Favori Akışı:

```
FavoritesPage.jsx → useFavorite.js → FavoriteService.js → API/localStorage
```

### Backend Favori Akışı:

```
FavoritesController.cs → IFavoriteService → FavoriteManager.cs → FavoriteRepository.cs → Database
```

## Entegrasyon Checklist

- [ ] Backend API endpoint'leri test edildi
- [ ] Auth sistemi localStorage entegrasyonu yapıldı
- [ ] `apiConfig.js` değerleri güncellendi
- [ ] Giriş yapmış kullanıcı testleri yapıldı
- [ ] Giriş yapmamış kullanıcı testleri yapıldı
- [ ] API hata durumu testleri yapıldı
- [ ] Console log'ları temizlendi (production için)

## Geliştirici Notları

- Sistem şu anda %100 localStorage ile çalışıyor
- Backend aktif olduğunda otomatik geçiş yapacak
- Hibrit sistem sayesinde her durumda çalışır
- Mimari clean ve genişletilebilir
- Debug modunda detaylı log'lar mevcut

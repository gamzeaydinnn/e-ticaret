# 🚀 SUNUCUDA GIT DIVERGENT BRANCHES SORUNU - ÇÖZÜM

## ⚠️ SORUN
```
fatal: Need to specify how to reconcile divergent branches.
```

Bu hata, yerel sunucu kodu ile GitHub kodu arasında çakışma olduğu anlamına gelir.

---

## ✅ ÇÖZÜM (3 SEÇENEK)

### SEÇENEK 1: Rebase Kullan (ÖNERİLEN)
```bash
git config pull.rebase true
git pull origin main
```

### SEÇENEK 2: Merge Kullan
```bash
git config pull.rebase false
git pull origin main
```

### SEÇENEK 3: Hard Reset (Bütün yerel değişiklikleri sil - DİKKAT!)
```bash
git fetch origin
git reset --hard origin/main
```

---

## 🎯 DEPLOYMENT SIRASIYLA YAPACAĞINız

Sunucuda şu komutları çalıştırın:

```bash
cd /home/huseyinadm/eticaret

# SEÇENEK 1 (ÖNERİLEN): Rebase ile çek
git config pull.rebase true
git pull origin main

# VEYA SEÇENEK 3 (Eğer local değişiklik yoksa):
git fetch origin
git reset --hard origin/main
```

Bundan sonra normal deployment devam eder:

```bash
docker-compose -f docker-compose.prod.yml build --no-cache
docker-compose -f docker-compose.prod.yml up -d
```

---

## 📌 HIZLI REFERANS

**Divergent branches hatası alırsanız:**
1. Rebase ile çekin: `git config pull.rebase true && git pull origin main`
2. Veya Hard reset: `git reset --hard origin/main`
3. Sonra normal deployment devam eder

---

**Şimdi deploymen'ti devam ettirebilirsiniz!**

// scripts/reset-mock-db.js
// mock-db.json'u varsayılan değerlere sıfırlar

const fs = require('fs');
const path = require('path');

const DEFAULTS = path.join(__dirname, '..', 'mock-db.defaults.json');
const TARGET = path.join(__dirname, '..', 'mock-db.json');

console.log('🔄 Mock veritabanı sıfırlanıyor...');

fs.copyFile(DEFAULTS, TARGET, (err) => {
  if (err) {
    console.error('❌ DB reset hatası:', err);
    process.exit(1);
  }
  console.log('✅ mock-db.json -> varsayılana döndürüldü!');
  console.log('📁 Dosya konumu:', TARGET);
});

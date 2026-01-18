// ============================================================
// SESSION TIMEOUT HOOK - Admin Panel Oturum Yönetimi
// ============================================================
// Bu hook, kullanıcı aktivitesini izler ve belirli bir süre
// hareketsizlik sonrası otomatik çıkış yapar.
// GÜVENLİK: Admin paneline yetkisiz erişimi önler.
// ============================================================

import { useEffect, useCallback, useRef, useState } from "react";

/**
 * Session Timeout Hook
 *
 * @param {Object} options - Hook seçenekleri
 * @param {number} options.timeoutMinutes - Zaman aşımı süresi (dakika) - Varsayılan: 30
 * @param {number} options.warningMinutes - Uyarı gösterilecek kalan süre (dakika) - Varsayılan: 5
 * @param {Function} options.onTimeout - Zaman aşımında çağrılacak fonksiyon (logout)
 * @param {Function} options.onWarning - Uyarı anında çağrılacak fonksiyon
 *
 * @returns {Object} { remainingTime, isWarning, resetTimer, extendSession }
 */
const useSessionTimeout = ({
  timeoutMinutes = 30,
  warningMinutes = 5,
  onTimeout,
  onWarning,
} = {}) => {
  // ============================================================
  // STATE VE REFERANSLAR
  // ============================================================

  // Kalan süre (saniye cinsinden)
  const [remainingTime, setRemainingTime] = useState(timeoutMinutes * 60);

  // Uyarı durumu - son X dakika kaldığında true olur
  const [isWarning, setIsWarning] = useState(false);

  // Timer referansları - cleanup için gerekli
  const countdownRef = useRef(null);
  const lastActivityRef = useRef(Date.now());

  // Uyarı callback'i bir kez çağrılsın diye flag
  const warningCalledRef = useRef(false);

  // ============================================================
  // TIMER SIFIRLAMA
  // ============================================================
  // Kullanıcı aktivitesi algılandığında timer'ı sıfırlar
  const resetTimer = useCallback(() => {
    lastActivityRef.current = Date.now();
    setRemainingTime(timeoutMinutes * 60);
    setIsWarning(false);
    warningCalledRef.current = false;
  }, [timeoutMinutes]);

  // ============================================================
  // OTURUM UZATMA
  // ============================================================
  // Kullanıcı "Oturumu Uzat" dediğinde çağrılır
  const extendSession = useCallback(() => {
    resetTimer();
    console.log("🔄 Oturum süresi uzatıldı");
  }, [resetTimer]);

  // ============================================================
  // AKTİVİTE DİNLEYİCİLERİ
  // ============================================================
  useEffect(() => {
    // İzlenecek kullanıcı aktiviteleri
    // Mouse hareketi, klavye, tıklama, scroll, dokunma
    const activityEvents = [
      "mousedown",
      "mousemove",
      "keydown",
      "scroll",
      "touchstart",
      "click",
    ];

    // Performans için throttle uyguluyoruz
    // Her aktivitede değil, en fazla 1 saniyede bir sıfırlama
    let lastReset = Date.now();
    const throttleMs = 1000;

    const handleActivity = () => {
      const now = Date.now();
      if (now - lastReset > throttleMs) {
        lastReset = now;
        resetTimer();
      }
    };

    // Event listener'ları ekle
    activityEvents.forEach((event) => {
      window.addEventListener(event, handleActivity, { passive: true });
    });

    // Cleanup
    return () => {
      activityEvents.forEach((event) => {
        window.removeEventListener(event, handleActivity);
      });
    };
  }, [resetTimer]);

  // ============================================================
  // COUNTDOWN TIMER
  // ============================================================
  useEffect(() => {
    // Her saniye kalan süreyi güncelle
    countdownRef.current = setInterval(() => {
      const elapsed = Math.floor((Date.now() - lastActivityRef.current) / 1000);
      const remaining = Math.max(0, timeoutMinutes * 60 - elapsed);

      setRemainingTime(remaining);

      // Uyarı eşiğine ulaşıldı mı?
      const warningThreshold = warningMinutes * 60;
      if (remaining <= warningThreshold && remaining > 0) {
        setIsWarning(true);

        // Uyarı callback'i sadece bir kez çağır
        if (!warningCalledRef.current && onWarning) {
          warningCalledRef.current = true;
          onWarning(remaining);
        }
      }

      // Süre doldu - timeout callback'i çağır
      if (remaining === 0) {
        if (countdownRef.current) {
          clearInterval(countdownRef.current);
        }
        if (onTimeout) {
          console.log("⏰ Oturum süresi doldu - Otomatik çıkış yapılıyor");
          onTimeout();
        }
      }
    }, 1000);

    // Cleanup
    return () => {
      if (countdownRef.current) {
        clearInterval(countdownRef.current);
      }
    };
  }, [timeoutMinutes, warningMinutes, onTimeout, onWarning]);

  // ============================================================
  // HELPER FONKSIYONLAR
  // ============================================================

  // Kalan süreyi formatla (MM:SS)
  const formatTime = useCallback((seconds) => {
    const mins = Math.floor(seconds / 60);
    const secs = seconds % 60;
    return `${mins}:${secs.toString().padStart(2, "0")}`;
  }, []);

  // ============================================================
  // RETURN
  // ============================================================
  return {
    remainingTime, // Kalan süre (saniye)
    remainingTimeFormatted: formatTime(remainingTime), // Formatlanmış süre
    isWarning, // Uyarı modu aktif mi?
    resetTimer, // Timer'ı sıfırla
    extendSession, // Oturumu uzat
  };
};

export default useSessionTimeout;

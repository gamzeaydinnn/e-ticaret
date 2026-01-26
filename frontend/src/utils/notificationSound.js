/**
 * Notification Sound Utility
 *
 * Bildirim sesleri için yardımcı fonksiyonlar.
 * WAV dosyası yoksa Web Audio API ile fallback ses oluşturur.
 */

// Ses dosyası yolları - Mixkit ücretsiz sesler
const SOUND_PATHS = {
  newOrder: "/sounds/mixkit-melodic-race-countdown-1955.wav", // Yeni sipariş - dikkat çekici
  notification: "/sounds/mixkit-bell-notification-933.wav", // Genel bildirim
  orderReady: "/sounds/mixkit-happy-bells-notification-937.wav", // Sipariş hazır - mutlu ses
  assigned: "/sounds/mixkit-bell-notification-933.wav", // Atama bildirimi
  alert: "/sounds/mixkit-melodic-race-countdown-1955.wav", // Uyarı
  success: "/sounds/mixkit-happy-bells-notification-937.wav", // Başarı
  error: "/sounds/mixkit-bell-notification-933.wav", // Hata
};

// AudioContext singleton
let audioContext = null;

/**
 * Web Audio API ile basit beep sesi oluşturur
 * @param {number} frequency - Frekans (Hz)
 * @param {number} duration - Süre (ms)
 * @param {string} type - Dalga tipi ('sine', 'square', 'sawtooth', 'triangle')
 */
export const playBeep = (frequency = 800, duration = 200, type = "sine") => {
  try {
    if (!audioContext) {
      audioContext = new (window.AudioContext || window.webkitAudioContext)();
    }

    const oscillator = audioContext.createOscillator();
    const gainNode = audioContext.createGain();

    oscillator.connect(gainNode);
    gainNode.connect(audioContext.destination);

    oscillator.frequency.value = frequency;
    oscillator.type = type;

    // Fade out efekti
    gainNode.gain.setValueAtTime(0.3, audioContext.currentTime);
    gainNode.gain.exponentialRampToValueAtTime(
      0.01,
      audioContext.currentTime + duration / 1000,
    );

    oscillator.start(audioContext.currentTime);
    oscillator.stop(audioContext.currentTime + duration / 1000);

    return true;
  } catch (error) {
    console.warn("[Sound] Web Audio API hatası:", error);
    return false;
  }
};

/**
 * Bildirim sesi çalar
 * MP3 dosyası yoksa fallback beep sesi kullanır
 * @param {string} soundType - Ses tipi (newOrder, notification, alert, vb.)
 * @param {number} volume - Ses seviyesi (0-1)
 */
export const playNotificationSound = async (
  soundType = "notification",
  volume = 0.7,
) => {
  // Kullanıcı ses ayarını kontrol et
  const soundEnabled =
    localStorage.getItem("notificationSoundEnabled") !== "false";
  if (!soundEnabled) return false;

  const soundPath = SOUND_PATHS[soundType] || SOUND_PATHS.notification;

  try {
    const audio = new Audio(soundPath);
    audio.volume = Math.min(1, Math.max(0, volume));

    // Ses dosyası yüklenebilir mi kontrol et
    await new Promise((resolve, reject) => {
      audio.oncanplaythrough = resolve;
      audio.onerror = reject;
      audio.load();

      // 2 saniye timeout
      setTimeout(reject, 2000);
    });

    await audio.play();
    return true;
  } catch (error) {
    console.warn(`[Sound] ${soundPath} çalınamadı, fallback beep kullanılıyor`);

    // Fallback: Web Audio API ile beep
    const beepConfig = {
      newOrder: { freq: 880, duration: 300, type: "sine" },
      notification: { freq: 660, duration: 200, type: "sine" },
      orderReady: { freq: 523, duration: 250, type: "triangle" },
      assigned: { freq: 784, duration: 200, type: "sine" },
      alert: { freq: 440, duration: 400, type: "square" },
      success: { freq: 880, duration: 150, type: "sine" },
      error: { freq: 220, duration: 500, type: "sawtooth" },
    };

    const config = beepConfig[soundType] || beepConfig.notification;

    // İki kez beep (dikkat çekmesi için)
    playBeep(config.freq, config.duration, config.type);
    setTimeout(
      () => playBeep(config.freq * 1.2, config.duration, config.type),
      config.duration + 50,
    );

    return true;
  }
};

/**
 * Yeni sipariş sesi (daha dikkat çekici)
 */
export const playNewOrderSound = () => {
  return playNotificationSound("newOrder", 0.8);
};

/**
 * Sipariş hazır sesi
 */
export const playOrderReadySound = () => {
  return playNotificationSound("orderReady", 0.7);
};

/**
 * Uyarı sesi
 */
export const playAlertSound = () => {
  return playNotificationSound("alert", 0.6);
};

/**
 * Başarı sesi
 */
export const playSuccessSound = () => {
  return playNotificationSound("success", 0.5);
};

/**
 * Browser notification göster (izin varsa)
 * @param {string} title - Başlık
 * @param {string} body - İçerik
 * @param {object} options - Ek seçenekler
 */
export const showBrowserNotification = (title, body, options = {}) => {
  if (!("Notification" in window)) {
    console.warn("[Notification] Bu tarayıcı bildirimleri desteklemiyor");
    return false;
  }

  if (Notification.permission === "granted") {
    const notification = new Notification(title, {
      body,
      icon: options.icon || "/logo192.png",
      tag: options.tag || "order-notification",
      requireInteraction: options.requireInteraction || false,
      silent: options.silent || false,
      ...options,
    });

    // Tıklama işleyicisi
    if (options.onClick) {
      notification.onclick = options.onClick;
    }

    // Otomatik kapanma
    if (options.autoClose !== false) {
      setTimeout(() => notification.close(), options.autoCloseDelay || 5000);
    }

    return notification;
  } else if (Notification.permission !== "denied") {
    Notification.requestPermission().then((permission) => {
      if (permission === "granted") {
        showBrowserNotification(title, body, options);
      }
    });
  }

  return null;
};

/**
 * Bildirim izni iste
 */
export const requestNotificationPermission = async () => {
  if (!("Notification" in window)) {
    return "unsupported";
  }

  if (Notification.permission === "granted") {
    return "granted";
  }

  if (Notification.permission === "denied") {
    return "denied";
  }

  const permission = await Notification.requestPermission();
  return permission;
};

export default {
  playNotificationSound,
  playNewOrderSound,
  playOrderReadySound,
  playAlertSound,
  playSuccessSound,
  playBeep,
  showBrowserNotification,
  requestNotificationPermission,
  SOUND_PATHS,
};

/**
 * SignalR Client Servisi
 * 
 * Bu servis, backend SignalR hub'larına bağlantı kurar ve real-time olayları yönetir.
 * Özellikler:
 * - Otomatik yeniden bağlanma (exponential backoff)
 * - Connection state yönetimi
 * - Event subscription/unsubscription
 * - Admin ve Courier hub'ları için ayrı bağlantılar
 * 
 * NEDEN SignalR: WebSocket tabanlı real-time iletişim için Microsoft'un standart çözümü.
 * Automatic reconnection, fallback protocols (SSE, Long Polling) ve grup yönetimi sağlar.
 */

import * as signalR from "@microsoft/signalr";

// ============================================================================
// SABITLER VE KONFİGÜRASYON
// ============================================================================

/**
 * SignalR Hub URL'leri
 * NEDEN ayrı hub'lar: Admin ve Courier farklı yetkilere sahip, güvenlik için ayrılmalı
 */
const HUB_URLS = {
  delivery: "/hubs/delivery",           // Teslimat işlemleri için ana hub
  adminNotification: "/hubs/admin",     // Admin bildirimleri
  courierNotification: "/hubs/courier"  // Kurye bildirimleri
};

/**
 * Bağlantı durumu enum'u
 * NEDEN: UI'da bağlantı durumunu göstermek ve aksiyonları buna göre yönetmek için
 */
export const ConnectionState = {
  DISCONNECTED: "disconnected",
  CONNECTING: "connecting",
  CONNECTED: "connected",
  RECONNECTING: "reconnecting",
  FAILED: "failed"
};

/**
 * SignalR Event Tipleri
 * NEDEN: Backend ile frontend arasında tutarlı event isimlendirmesi için
 */
export const SignalREvents = {
  // Teslimat Olayları
  DELIVERY_CREATED: "DeliveryCreated",
  DELIVERY_ASSIGNED: "DeliveryAssigned",
  DELIVERY_STATUS_CHANGED: "DeliveryStatusChanged",
  DELIVERY_COMPLETED: "DeliveryCompleted",
  DELIVERY_FAILED: "DeliveryFailed",
  DELIVERY_CANCELLED: "DeliveryCancelled",
  
  // Kurye Olayları
  COURIER_LOCATION_UPDATED: "CourierLocationUpdated",
  COURIER_STATUS_CHANGED: "CourierStatusChanged",
  COURIER_ASSIGNED_TASK: "CourierAssignedTask",
  COURIER_ONLINE: "CourierOnline",
  COURIER_OFFLINE: "CourierOffline",
  
  // Sipariş Olayları
  ORDER_CREATED: "OrderCreated",
  ORDER_STATUS_CHANGED: "OrderStatusChanged",
  ORDER_READY_FOR_DISPATCH: "OrderReadyForDispatch",
  
  // Sistem Olayları
  NOTIFICATION: "Notification",
  ALERT: "Alert"
};

// ============================================================================
// SİGNALR HUB BAĞLANTI SINIFI
// ============================================================================

/**
 * SignalR Hub bağlantı yöneticisi
 * 
 * SOLID Prensipleri:
 * - Single Responsibility: Sadece bağlantı yönetimi
 * - Open/Closed: Event listeners ile genişletilebilir
 * - Dependency Inversion: Token provider dışarıdan enjekte edilebilir
 */
class SignalRHubConnection {
  constructor(hubUrl, options = {}) {
    this.hubUrl = hubUrl;
    this.connection = null;
    this.connectionState = ConnectionState.DISCONNECTED;
    this.eventHandlers = new Map();
    this.stateChangeCallbacks = [];
    this.reconnectAttempts = 0;
    this.maxReconnectAttempts = options.maxReconnectAttempts || 10;
    this.tokenProvider = options.tokenProvider || this.defaultTokenProvider;
    
    // Reconnection delay (exponential backoff)
    this.baseReconnectDelay = 1000;
    this.maxReconnectDelay = 30000;
  }

  /**
   * Varsayılan token sağlayıcı
   * NEDEN: JWT token'ı localStorage'dan alarak SignalR bağlantısına ekler
   */
  defaultTokenProvider = () => {
    return localStorage.getItem("token") || 
           localStorage.getItem("authToken") || 
           localStorage.getItem("adminToken") ||
           localStorage.getItem("courierToken");
  };

  /**
   * Hub bağlantısını başlatır
   * NEDEN: Lazy initialization - bağlantı ihtiyaç olduğunda kurulur
   */
  async start() {
    if (this.connectionState === ConnectionState.CONNECTED) {
      console.log(`[SignalR] ${this.hubUrl} zaten bağlı`);
      return true;
    }

    try {
      this.updateState(ConnectionState.CONNECTING);
      
      // API base URL'i belirle
      const baseUrl = process.env.REACT_APP_API_URL || "";
      const fullUrl = `${baseUrl}${this.hubUrl}`;

      // SignalR bağlantısı oluştur
      this.connection = new signalR.HubConnectionBuilder()
        .withUrl(fullUrl, {
          // JWT token'ı access token olarak gönder
          accessTokenFactory: () => this.tokenProvider()
        })
        .withAutomaticReconnect({
          // Özel reconnect stratejisi: exponential backoff
          nextRetryDelayInMilliseconds: (retryContext) => {
            if (retryContext.previousRetryCount >= this.maxReconnectAttempts) {
              return null; // Yeniden denemeyi durdur
            }
            const delay = Math.min(
              this.baseReconnectDelay * Math.pow(2, retryContext.previousRetryCount),
              this.maxReconnectDelay
            );
            console.log(`[SignalR] ${this.hubUrl} - Yeniden bağlanma denemesi ${retryContext.previousRetryCount + 1}, ${delay}ms sonra`);
            return delay;
          }
        })
        .configureLogging(signalR.LogLevel.Information)
        .build();

      // Bağlantı olaylarını dinle
      this.setupConnectionHandlers();

      // Bağlantıyı başlat
      await this.connection.start();
      
      this.reconnectAttempts = 0;
      this.updateState(ConnectionState.CONNECTED);
      console.log(`[SignalR] ${this.hubUrl} bağlantısı kuruldu ✓`);
      
      return true;
    } catch (error) {
      console.error(`[SignalR] ${this.hubUrl} bağlantı hatası:`, error);
      this.updateState(ConnectionState.FAILED);
      return false;
    }
  }

  /**
   * Bağlantı olay işleyicilerini kurar
   * NEDEN: Bağlantı durumu değişikliklerini yakalamak için
   */
  setupConnectionHandlers() {
    // Bağlantı kapandığında
    this.connection.onclose((error) => {
      console.log(`[SignalR] ${this.hubUrl} bağlantısı kapandı`, error || "");
      this.updateState(ConnectionState.DISCONNECTED);
    });

    // Yeniden bağlanma başladığında
    this.connection.onreconnecting((error) => {
      console.log(`[SignalR] ${this.hubUrl} yeniden bağlanıyor...`, error || "");
      this.updateState(ConnectionState.RECONNECTING);
    });

    // Yeniden bağlandığında
    this.connection.onreconnected((connectionId) => {
      console.log(`[SignalR] ${this.hubUrl} yeniden bağlandı. Connection ID: ${connectionId}`);
      this.updateState(ConnectionState.CONNECTED);
      this.reconnectAttempts = 0;
    });
  }

  /**
   * Bağlantı durumunu günceller ve callback'leri çağırır
   */
  updateState(newState) {
    const oldState = this.connectionState;
    this.connectionState = newState;
    
    // Tüm state change callback'lerini çağır
    this.stateChangeCallbacks.forEach(callback => {
      try {
        callback(newState, oldState);
      } catch (error) {
        console.error("[SignalR] State change callback hatası:", error);
      }
    });
  }

  /**
   * Hub bağlantısını durdurur
   */
  async stop() {
    if (this.connection) {
      try {
        await this.connection.stop();
        console.log(`[SignalR] ${this.hubUrl} bağlantısı kapatıldı`);
      } catch (error) {
        console.error(`[SignalR] ${this.hubUrl} bağlantı kapatma hatası:`, error);
      }
    }
    this.updateState(ConnectionState.DISCONNECTED);
  }

  /**
   * Event dinleyicisi ekler
   * NEDEN: Belirli olayları dinlemek için subscribe pattern
   * 
   * @param {string} eventName - Dinlenecek event adı
   * @param {Function} handler - Event handler fonksiyonu
   * @returns {Function} Unsubscribe fonksiyonu
   */
  on(eventName, handler) {
    if (!this.connection) {
      console.warn(`[SignalR] ${this.hubUrl} - Bağlantı yok, event handler bekletildi: ${eventName}`);
      
      // Handler'ı kaydet, bağlantı kurulunca eklenecek
      if (!this.eventHandlers.has(eventName)) {
        this.eventHandlers.set(eventName, []);
      }
      this.eventHandlers.get(eventName).push(handler);
      
      return () => this.off(eventName, handler);
    }

    // Bağlantı varsa direkt ekle
    this.connection.on(eventName, handler);
    
    // Handler'ı kaydet (reconnect sonrası için)
    if (!this.eventHandlers.has(eventName)) {
      this.eventHandlers.set(eventName, []);
    }
    this.eventHandlers.get(eventName).push(handler);

    // Unsubscribe fonksiyonu döndür
    return () => this.off(eventName, handler);
  }

  /**
   * Event dinleyicisini kaldırır
   */
  off(eventName, handler) {
    if (this.connection) {
      this.connection.off(eventName, handler);
    }
    
    const handlers = this.eventHandlers.get(eventName);
    if (handlers) {
      const index = handlers.indexOf(handler);
      if (index > -1) {
        handlers.splice(index, 1);
      }
    }
  }

  /**
   * Hub'a mesaj gönderir
   * 
   * @param {string} methodName - Hub method adı
   * @param  {...any} args - Method argümanları
   */
  async invoke(methodName, ...args) {
    if (!this.connection || this.connectionState !== ConnectionState.CONNECTED) {
      throw new Error(`[SignalR] ${this.hubUrl} - Bağlantı yok, invoke yapılamıyor: ${methodName}`);
    }

    try {
      return await this.connection.invoke(methodName, ...args);
    } catch (error) {
      console.error(`[SignalR] ${this.hubUrl} invoke hatası (${methodName}):`, error);
      throw error;
    }
  }

  /**
   * Hub'a tek yönlü mesaj gönderir (yanıt beklemez)
   */
  send(methodName, ...args) {
    if (!this.connection || this.connectionState !== ConnectionState.CONNECTED) {
      console.warn(`[SignalR] ${this.hubUrl} - Bağlantı yok, send atlandı: ${methodName}`);
      return;
    }

    this.connection.send(methodName, ...args).catch(error => {
      console.error(`[SignalR] ${this.hubUrl} send hatası (${methodName}):`, error);
    });
  }

  /**
   * State değişikliği callback'i ekler
   */
  onStateChange(callback) {
    this.stateChangeCallbacks.push(callback);
    return () => {
      const index = this.stateChangeCallbacks.indexOf(callback);
      if (index > -1) {
        this.stateChangeCallbacks.splice(index, 1);
      }
    };
  }

  /**
   * Mevcut bağlantı durumunu döndürür
   */
  getState() {
    return this.connectionState;
  }

  /**
   * Bağlantının aktif olup olmadığını kontrol eder
   */
  isConnected() {
    return this.connectionState === ConnectionState.CONNECTED;
  }
}

// ============================================================================
// SİGNALR SERVİS YÖNETİCİSİ (SINGLETON)
// ============================================================================

/**
 * Ana SignalR servis yöneticisi
 * NEDEN Singleton: Tüm uygulama için tek bir bağlantı havuzu yönetimi
 */
class SignalRService {
  constructor() {
    this.connections = new Map();
    this.globalNotificationHandlers = [];
  }

  /**
   * Admin için hub bağlantısı kurar
   */
  async connectAdmin() {
    // Delivery hub'ına bağlan
    const deliveryHub = this.getOrCreateConnection(HUB_URLS.delivery);
    const adminHub = this.getOrCreateConnection(HUB_URLS.adminNotification);
    
    const results = await Promise.all([
      deliveryHub.start(),
      adminHub.start()
    ]);

    // Admin grubuna katıl
    if (deliveryHub.isConnected()) {
      try {
        await deliveryHub.invoke("JoinAdminGroup");
        console.log("[SignalR] Admin grubuna katıldı");
      } catch (error) {
        console.warn("[SignalR] Admin grubuna katılma hatası:", error);
      }
    }

    return results.every(r => r);
  }

  /**
   * Kurye için hub bağlantısı kurar
   */
  async connectCourier(courierId) {
    if (!courierId) {
      throw new Error("[SignalR] Kurye ID gerekli");
    }

    const deliveryHub = this.getOrCreateConnection(HUB_URLS.delivery, {
      tokenProvider: () => localStorage.getItem("courierToken")
    });
    const courierHub = this.getOrCreateConnection(HUB_URLS.courierNotification, {
      tokenProvider: () => localStorage.getItem("courierToken")
    });

    const results = await Promise.all([
      deliveryHub.start(),
      courierHub.start()
    ]);

    // Kurye grubuna katıl
    if (deliveryHub.isConnected()) {
      try {
        await deliveryHub.invoke("JoinCourierGroup", courierId);
        console.log(`[SignalR] Kurye #${courierId} grubuna katıldı`);
      } catch (error) {
        console.warn("[SignalR] Kurye grubuna katılma hatası:", error);
      }
    }

    return results.every(r => r);
  }

  /**
   * Bağlantı yoksa oluşturur, varsa mevcut olanı döndürür
   */
  getOrCreateConnection(hubUrl, options = {}) {
    if (!this.connections.has(hubUrl)) {
      this.connections.set(hubUrl, new SignalRHubConnection(hubUrl, options));
    }
    return this.connections.get(hubUrl);
  }

  /**
   * Belirli bir hub'a erişim sağlar
   */
  getHub(hubUrl) {
    return this.connections.get(hubUrl);
  }

  /**
   * Delivery hub'ına hızlı erişim
   */
  get deliveryHub() {
    return this.getOrCreateConnection(HUB_URLS.delivery);
  }

  /**
   * Admin hub'ına hızlı erişim
   */
  get adminHub() {
    return this.getOrCreateConnection(HUB_URLS.adminNotification);
  }

  /**
   * Courier hub'ına hızlı erişim
   */
  get courierHub() {
    return this.getOrCreateConnection(HUB_URLS.courierNotification);
  }

  /**
   * Tüm bağlantıları kapatır
   */
  async disconnectAll() {
    const stopPromises = [];
    this.connections.forEach((connection) => {
      stopPromises.push(connection.stop());
    });
    await Promise.all(stopPromises);
    this.connections.clear();
    console.log("[SignalR] Tüm bağlantılar kapatıldı");
  }

  /**
   * Event dinleyicisi ekler (kolaylık metodu)
   */
  on(hubUrl, eventName, handler) {
    const hub = this.getOrCreateConnection(hubUrl);
    return hub.on(eventName, handler);
  }

  /**
   * Delivery olayı dinler
   */
  onDeliveryEvent(eventName, handler) {
    return this.on(HUB_URLS.delivery, eventName, handler);
  }

  /**
   * Admin bildirimi dinler
   */
  onAdminNotification(handler) {
    return this.on(HUB_URLS.adminNotification, SignalREvents.NOTIFICATION, handler);
  }

  /**
   * Kurye bildirimi dinler
   */
  onCourierNotification(handler) {
    return this.on(HUB_URLS.courierNotification, SignalREvents.NOTIFICATION, handler);
  }

  /**
   * Genel bağlantı durumunu döndürür
   */
  getConnectionStatus() {
    const status = {};
    this.connections.forEach((connection, url) => {
      status[url] = connection.getState();
    });
    return status;
  }

  /**
   * En az bir bağlantının aktif olup olmadığını kontrol eder
   */
  isAnyConnected() {
    let connected = false;
    this.connections.forEach((connection) => {
      if (connection.isConnected()) {
        connected = true;
      }
    });
    return connected;
  }
}

// ============================================================================
// SINGLETON INSTANCE VE EXPORT
// ============================================================================

// Singleton instance oluştur
const signalRService = new SignalRService();

// Geliştirme ortamında global erişim (debug için)
if (process.env.NODE_ENV === "development") {
  window.__signalR = signalRService;
}

// Named exports
export { 
  signalRService, 
  HUB_URLS, 
  SignalRHubConnection 
};

// Default export
export default signalRService;

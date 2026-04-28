/**
 * useStockUpdates - Anlık stok ve fiyat güncellemelerini dinleyen React hook.
 *
 * NEDEN: Ürün detay sayfası veya sepette iken stok tükenirse / fiyat değişirse
 * kullanıcı sayfa yenilemeden bilgilendirilmeli. Backend'deki HotPoll servisi
 * 10sn'de bir Mikro ERP'den delta çekiyor ve StockHub üzerinden push yapıyor.
 *
 * KULLANIM:
 *   const { stockQuantity, price, isOutOfStock, lastUpdate } = useStockUpdates(productId);
 *
 * NASIL ÇALIŞIR:
 * 1. Mount olduğunda SignalR StockHub'a bağlanır
 * 2. İlgili ürün odasına (product-{id}) katılır
 * 3. StockChanged ve PriceChanged eventlerini dinler
 * 4. Unmount olduğunda odadan ayrılır (cleanup)
 */

import { useEffect, useState, useCallback, useRef } from "react";
import { signalRService, SignalREvents } from "../services/signalRService";

/**
 * Tekil ürün için anlık stok/fiyat güncellemelerini dinler.
 *
 * @param {number} productId - İzlenecek ürün ID'si
 * @param {Object} options - Seçenekler
 * @param {number} options.initialStock - Başlangıç stok miktarı
 * @param {number} options.initialPrice - Başlangıç fiyatı
 * @param {Function} options.onStockChange - Stok değiştiğinde callback
 * @param {Function} options.onPriceChange - Fiyat değiştiğinde callback
 * @param {Function} options.onOutOfStock - Stok tükendiğinde callback
 * @returns {Object} Güncel stok/fiyat/durum bilgileri
 */
export function useStockUpdates(productId, options = {}) {
  const {
    initialStock = null,
    initialPrice = null,
    onStockChange,
    onPriceChange,
    onOutOfStock,
  } = options;

  const [stockQuantity, setStockQuantity] = useState(initialStock);
  const [price, setPrice] = useState(initialPrice);
  const [isOutOfStock, setIsOutOfStock] = useState(false);
  const [lastUpdate, setLastUpdate] = useState(null);
  const [isConnected, setIsConnected] = useState(false);

  // Callback ref'leri — stale closure'ı önlemek için
  const onStockChangeRef = useRef(onStockChange);
  const onPriceChangeRef = useRef(onPriceChange);
  const onOutOfStockRef = useRef(onOutOfStock);

  useEffect(() => {
    onStockChangeRef.current = onStockChange;
    onPriceChangeRef.current = onPriceChange;
    onOutOfStockRef.current = onOutOfStock;
  }, [onStockChange, onPriceChange, onOutOfStock]);

  // initialStock/initialPrice değiştiğinde state'i güncelle
  useEffect(() => {
    if (initialStock !== null) setStockQuantity(initialStock);
  }, [initialStock]);

  useEffect(() => {
    if (initialPrice !== null) setPrice(initialPrice);
  }, [initialPrice]);

  useEffect(() => {
    if (!productId || productId <= 0) return;

    let mounted = true;
    const unsubscribers = [];

    const setup = async () => {
      // StockHub'a bağlan ve ürün odasına katıl
      const joined = await signalRService.joinProductRoom(productId);
      if (!mounted) return;
      setIsConnected(joined);

      // Stok değişikliği dinle
      const unsubStock = signalRService.onStockChanged((data) => {
        if (!mounted || data.productId !== productId) return;

        setStockQuantity(data.newQuantity);
        setIsOutOfStock(data.isOutOfStock || data.newQuantity <= 0);
        setLastUpdate(new Date(data.timestamp));

        // Custom callback
        onStockChangeRef.current?.(data);

        // Stok tükendiyse özel callback
        if (data.isOutOfStock || data.newQuantity <= 0) {
          onOutOfStockRef.current?.(data);
        }
      });
      unsubscribers.push(unsubStock);

      // Fiyat değişikliği dinle
      const unsubPrice = signalRService.onPriceChanged((data) => {
        if (!mounted || data.productId !== productId) return;

        setPrice(data.newPrice);
        setLastUpdate(new Date(data.timestamp));
        onPriceChangeRef.current?.(data);
      });
      unsubscribers.push(unsubPrice);
    };

    setup();

    // Cleanup: odadan ayrıl ve event listener'ları kaldır
    return () => {
      mounted = false;
      signalRService.leaveProductRoom(productId);
      unsubscribers.forEach((unsub) => unsub?.());
    };
  }, [productId]);

  return {
    stockQuantity,
    price,
    isOutOfStock,
    lastUpdate,
    isConnected,
  };
}

/**
 * Sepetteki birden fazla ürünün stok durumunu toplu izler.
 *
 * NEDEN: Sepet sayfasında 1-50 arası ürün olabilir. Her biri için ayrı
 * bağlantı kurmak yerine JoinCartRooms ile toplu odaya katılır.
 *
 * @param {number[]} productIds - İzlenecek ürün ID'leri
 * @param {Function} onStockChange - Herhangi bir ürünün stoku değiştiğinde callback
 * @returns {Object} Ürün bazlı stok durumları map'i
 */
export function useCartStockUpdates(productIds, onStockChange) {
  // productId → { quantity, isOutOfStock } map
  const [stockMap, setStockMap] = useState({});
  const [priceMap, setPriceMap] = useState({});
  const [isConnected, setIsConnected] = useState(false);

  const onStockChangeRef = useRef(onStockChange);
  useEffect(() => {
    onStockChangeRef.current = onStockChange;
  }, [onStockChange]);

  useEffect(() => {
    if (!productIds?.length) return;

    let mounted = true;
    const unsubscribers = [];

    const setup = async () => {
      const joined = await signalRService.joinCartRooms(productIds);
      if (!mounted) return;
      setIsConnected(joined);

      // Stok değişikliği — hangi ürün değiştiyse state'e yaz
      const unsubStock = signalRService.onStockChanged((data) => {
        if (!mounted) return;

        // Sadece sepetteki ürünleri güncelle
        if (productIds.includes(data.productId)) {
          setStockMap((prev) => ({
            ...prev,
            [data.productId]: {
              quantity: data.newQuantity,
              isOutOfStock: data.isOutOfStock || data.newQuantity <= 0,
              previousQuantity: data.oldQuantity,
              timestamp: data.timestamp,
            },
          }));

          onStockChangeRef.current?.(data);
        }
      });
      unsubscribers.push(unsubStock);

      // Fiyat değişikliği
      const unsubPrice = signalRService.onPriceChanged((data) => {
        if (!mounted || !productIds.includes(data.productId)) return;

        setPriceMap((prev) => ({
          ...prev,
          [data.productId]: {
            price: data.newPrice,
            oldPrice: data.oldPrice,
            timestamp: data.timestamp,
          },
        }));
      });
      unsubscribers.push(unsubPrice);
    };

    setup();

    return () => {
      mounted = false;
      signalRService.leaveCartRooms(productIds);
      unsubscribers.forEach((unsub) => unsub?.());
    };
  }, [JSON.stringify(productIds)]); // productIds array değiştiğinde yeniden bağlan

  /**
   * Belirli ürünün güncel stok durumunu döner.
   * SignalR üzerinden güncelleme geldiyse onu, gelmediyse null döner.
   */
  const getStock = useCallback(
    (productId) => stockMap[productId] ?? null,
    [stockMap],
  );

  const getPrice = useCallback(
    (productId) => priceMap[productId] ?? null,
    [priceMap],
  );

  /**
   * Sepette stok tükenen ürün var mı kontrolü.
   */
  const hasOutOfStockItems = Object.values(stockMap).some(
    (s) => s.isOutOfStock,
  );

  return {
    stockMap,
    priceMap,
    getStock,
    getPrice,
    hasOutOfStockItems,
    isConnected,
  };
}

/**
 * Admin panelde global stok güncellemelerini dinler.
 * Tüm ürünlerin değişikliklerini realtime alır.
 *
 * @param {Object} options
 * @param {Function} options.onStockChange - Stok değiştiğinde
 * @param {Function} options.onPriceChange - Fiyat değiştiğinde
 * @param {number} options.maxHistory - Geçmiş kayıt limiti (varsayılan 50)
 * @returns {Object} Global stok güncelleme verileri
 */
export function useGlobalStockUpdates(options = {}) {
  const { onStockChange, onPriceChange, maxHistory = 50 } = options;

  const [updates, setUpdates] = useState([]);
  const [isConnected, setIsConnected] = useState(false);

  const onStockChangeRef = useRef(onStockChange);
  const onPriceChangeRef = useRef(onPriceChange);

  useEffect(() => {
    onStockChangeRef.current = onStockChange;
    onPriceChangeRef.current = onPriceChange;
  }, [onStockChange, onPriceChange]);

  useEffect(() => {
    let mounted = true;
    const unsubscribers = [];

    const setup = async () => {
      const joined = await signalRService.joinGlobalStockUpdates();
      if (!mounted) return;
      setIsConnected(joined);

      // Stok değişikliği
      const unsubStock = signalRService.onStockChanged((data) => {
        if (!mounted) return;

        const entry = { ...data, type: "stock", receivedAt: new Date() };

        setUpdates((prev) => {
          const next = [entry, ...prev];
          return next.slice(0, maxHistory); // Max kayıt sınırı
        });

        onStockChangeRef.current?.(data);
      });
      unsubscribers.push(unsubStock);

      // Fiyat değişikliği
      const unsubPrice = signalRService.onPriceChanged((data) => {
        if (!mounted) return;

        const entry = { ...data, type: "price", receivedAt: new Date() };

        setUpdates((prev) => {
          const next = [entry, ...prev];
          return next.slice(0, maxHistory);
        });

        onPriceChangeRef.current?.(data);
      });
      unsubscribers.push(unsubPrice);

      // Toplu güncelleme
      const unsubBulk = signalRService.onBulkStockUpdate((data) => {
        if (!mounted) return;

        const entries = (data.updates || []).map((u) => ({
          ...u,
          type: "bulk_stock",
          receivedAt: new Date(),
        }));

        setUpdates((prev) => {
          const next = [...entries, ...prev];
          return next.slice(0, maxHistory);
        });
      });
      unsubscribers.push(unsubBulk);
    };

    setup();

    return () => {
      mounted = false;
      // Global odadan ayrılmak için stock hub invoke
      const stockHub = signalRService.stockHub;
      if (stockHub?.isConnected()) {
        stockHub.invoke("LeaveGlobalStockUpdates").catch(() => {});
      }
      unsubscribers.forEach((unsub) => unsub?.());
    };
  }, [maxHistory]);

  const clearHistory = useCallback(() => setUpdates([]), []);

  return {
    updates,
    isConnected,
    clearHistory,
    totalUpdates: updates.length,
  };
}

export default useStockUpdates;

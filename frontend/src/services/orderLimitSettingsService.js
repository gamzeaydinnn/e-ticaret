import api from "./api";

const DEFAULT_ORDER_LIMIT_SETTINGS = {
  defaultMaxQuantityPiece: 5,
  defaultMinQuantityPiece: 1,
  defaultQuantityStepPiece: 1,
  defaultMaxWeightKg: 10,
  defaultMinWeightKg: 0.25,
  defaultWeightStepKg: 0.25,
};

let settingsCache = { data: null, timestamp: null };
const CACHE_TTL = 5 * 60 * 1000;

const normalizeSettings = (payload) => {
  const data = payload?.data || payload || {};
  return {
    ...DEFAULT_ORDER_LIMIT_SETTINGS,
    ...data,
    defaultMaxQuantityPiece: Number(
      data.defaultMaxQuantityPiece ?? DEFAULT_ORDER_LIMIT_SETTINGS.defaultMaxQuantityPiece,
    ),
    defaultMinQuantityPiece: Number(
      data.defaultMinQuantityPiece ?? DEFAULT_ORDER_LIMIT_SETTINGS.defaultMinQuantityPiece,
    ),
    defaultQuantityStepPiece: Number(
      data.defaultQuantityStepPiece ?? DEFAULT_ORDER_LIMIT_SETTINGS.defaultQuantityStepPiece,
    ),
    defaultMaxWeightKg: Number(
      data.defaultMaxWeightKg ?? DEFAULT_ORDER_LIMIT_SETTINGS.defaultMaxWeightKg,
    ),
    defaultMinWeightKg: Number(
      data.defaultMinWeightKg ?? DEFAULT_ORDER_LIMIT_SETTINGS.defaultMinWeightKg,
    ),
    defaultWeightStepKg: Number(
      data.defaultWeightStepKg ?? DEFAULT_ORDER_LIMIT_SETTINGS.defaultWeightStepKg,
    ),
  };
};

export const getOrderLimitSettings = async (forceRefresh = false) => {
  if (
    !forceRefresh &&
    settingsCache.data &&
    settingsCache.timestamp &&
    Date.now() - settingsCache.timestamp < CACHE_TTL
  ) {
    return settingsCache.data;
  }

  try {
    const response = await api.get("/api/ProductOrderLimitSettings/settings");
    const normalized = normalizeSettings(response);
    settingsCache = { data: normalized, timestamp: Date.now() };
    return normalized;
  } catch (error) {
    console.warn("[OrderLimitSettings] Ayarlar yüklenemedi:", error.message);
    return { ...DEFAULT_ORDER_LIMIT_SETTINGS };
  }
};

export const getOrderLimitSettingsAdmin = async () => {
  const response = await api.get("/api/ProductOrderLimitSettings/admin/settings");
  return normalizeSettings(response);
};

export const updateOrderLimitSettings = async (updateData) => {
  const response = await api.put(
    "/api/ProductOrderLimitSettings/admin/settings",
    updateData,
  );
  const normalized = normalizeSettings(response?.data || response);
  settingsCache = { data: normalized, timestamp: Date.now() };
  return normalized;
};

export default {
  getOrderLimitSettings,
  getOrderLimitSettingsAdmin,
  updateOrderLimitSettings,
};

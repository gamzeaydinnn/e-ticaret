import localRules from "./productCategoryRules.json";
import api from "../services/api";

let cachedRules = null;
let cacheTimestamp = 0;
const CACHE_TTL = 10 * 60 * 1000;

export const getProductCategoryRules = async () => {
  if (cachedRules && Date.now() - cacheTimestamp < CACHE_TTL) {
    return cachedRules;
  }

  try {
    const response = await api.get("/api/config/ProductCategoryRules");
    const rules = Array.isArray(response?.data)
      ? response.data
      : Array.isArray(response)
        ? response
        : null;

    if (rules && rules.length > 0) {
      cachedRules = rules;
      cacheTimestamp = Date.now();
      return rules;
    }
  } catch (error) {
    console.warn(
      "[ProductCategoryRules] API yüklenemedi, local JSON kullanılıyor:",
      error.message,
    );
  }

  cachedRules = localRules;
  cacheTimestamp = Date.now();
  return localRules;
};

export default getProductCategoryRules;

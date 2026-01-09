import localRules from "./productCategoryRules.json";

// Try to fetch from backend API. If not available, fall back to the local JSON.
export const getProductCategoryRules = async () => {
  try {
    const res = await fetch("/config/productCategoryRules");
    if (!res.ok) throw new Error("no api");
    const data = await res.json();
    return Array.isArray(data) ? data : localRules;
  } catch (err) {
    return localRules;
  }
};

export default getProductCategoryRules;

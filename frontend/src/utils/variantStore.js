// Simple client-side variant store using localStorage
const STORAGE_KEY = "productVariants_v1";

function readStore() {
  try {
    const raw = localStorage.getItem(STORAGE_KEY) || "{}";
    return JSON.parse(raw);
  } catch (e) {
    return {};
  }
}

function writeStore(obj) {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(obj));
  } catch (e) {
    // ignore
  }
}

export function getVariantsForProduct(productId) {
  const store = readStore();
  return store[productId] || [];
}

export function addVariant(productId, variant) {
  const store = readStore();
  const list = store[productId] || [];
  const id = Date.now();
  const toAdd = { id, ...variant };
  list.push(toAdd);
  store[productId] = list;
  writeStore(store);
  return toAdd;
}

export function removeVariant(productId, variantId) {
  const store = readStore();
  const list = (store[productId] || []).filter(
    (v) => String(v.id) !== String(variantId)
  );
  store[productId] = list;
  writeStore(store);
}

export function updateVariant(productId, variantId, patch) {
  const store = readStore();
  const list = (store[productId] || []).map((v) =>
    v.id === variantId ? { ...v, ...patch } : v
  );
  store[productId] = list;
  writeStore(store);
}

export function moveVariants(sourceKey, targetProductId) {
  const store = readStore();
  const sourceList = store[sourceKey] || [];
  if (!sourceList.length) return [];
  const destList = store[targetProductId] || [];
  // rekey ids to avoid collisions
  const moved = sourceList.map((v) => ({
    ...v,
    id: Date.now() + Math.floor(Math.random() * 1000),
  }));
  store[targetProductId] = destList.concat(moved);
  // remove source
  delete store[sourceKey];
  writeStore(store);
  return moved;
}
const variantStore = {
  getVariantsForProduct,
  addVariant,
  removeVariant,
  updateVariant,
  moveVariants,
};

export default variantStore;

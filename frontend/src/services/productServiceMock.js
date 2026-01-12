// Mock product service
const mockProducts = [
  {
    id: 1,
    name: "Örnek Ürün 1",
    price: 29.99,
    category: "Gıda",
    imageUrl: "/images/placeholder.png",
    stock: 100,
  },
  {
    id: 2,
    name: "Örnek Ürün 2",
    price: 49.99,
    category: "İçecek",
    imageUrl: "/images/placeholder.png",
    stock: 50,
  },
];

export const getAllProducts = async () => {
  return Promise.resolve(mockProducts);
};

export const getActive = async () => {
  return Promise.resolve(mockProducts);
};

export const getProductById = async (id) => {
  return Promise.resolve(mockProducts.find((p) => p.id === id));
};

export const getProductsByCategory = async (category) => {
  return Promise.resolve(mockProducts.filter((p) => p.category === category));
};

// Subscribe function for reactive updates
export const subscribe = (callback) => {
  // Mock subscription - no actual updates in static mock
  return () => {}; // Return unsubscribe function
};

const productServiceMock = {
  getAllProducts,
  getActive,
  getProductById,
  getProductsByCategory,
  subscribe,
  // Alias: adminService.js uyumluluğu için (getAll çağrısı yapıyor)
  getAll: getAllProducts,
};

export default productServiceMock;

// src/services/categoryService.js
// categoryServiceReal'ı re-export ediyoruz
// Bu dosya eksik olduğu için kategoriler yüklenmiyordu

import categoryServiceReal from "./categoryServiceReal";

// Default export olarak categoryServiceReal'ı kullan
const categoryService = categoryServiceReal;

// Named export for CategoryService (index.js compatibility)
export const CategoryService = categoryServiceReal;

export default categoryService;

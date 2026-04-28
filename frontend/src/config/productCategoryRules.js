import localRules from "./productCategoryRules.json";

// NEDEN: Backend'de /config/productCategoryRules endpoint'i yok; her seferinde
// 404 üretip konsülde gürültü yaratıyordu. Local JSON yeterli.
export const getProductCategoryRules = async () => localRules;

export default getProductCategoryRules;

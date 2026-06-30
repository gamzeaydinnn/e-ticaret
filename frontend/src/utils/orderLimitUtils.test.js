import {
  resolveProductOrderRule,
  clampQuantityToRule,
  DEFAULT_PIECE_RULE,
  DEFAULT_KG_RULE,
} from "./orderLimitUtils";

describe("orderLimitUtils", () => {
  test("backend orderLimits önceliklidir", () => {
    const rule = resolveProductOrderRule({
      orderLimits: {
        unit: "adet",
        minQuantity: 2,
        maxQuantity: 4,
        step: 1,
      },
    });
    expect(rule.max_quantity).toBe(4);
    expect(rule.min_quantity).toBe(2);
  });

  test("kg ürünlerde varsayılan 10 kg", () => {
    const rule = resolveProductOrderRule({ isWeightBased: true });
    expect(rule.unit).toBe("kg");
    expect(rule.max_quantity).toBe(DEFAULT_KG_RULE.max_quantity);
  });

  test("adet ürünlerde varsayılan max 5", () => {
    const rule = resolveProductOrderRule({ name: "Su" });
    expect(rule.max_quantity).toBe(DEFAULT_PIECE_RULE.max_quantity);
  });

  test("varyant max önceliklidir", () => {
    const rule = resolveProductOrderRule(
      { maxOrderQuantity: 8 },
      { maxOrderQuantity: 3 },
    );
    expect(rule.max_quantity).toBe(3);
  });

  test("kg ürün maxOrderQuantity olsa bile kg limiti uygulanır", () => {
    const rule = resolveProductOrderRule({
      name: "Domates",
      categoryName: "Meyve Sebze",
      maxOrderQuantity: 5,
      isWeightBased: true,
    });
    expect(rule.unit).toBe("kg");
    expect(rule.max_quantity).toBe(DEFAULT_KG_RULE.max_quantity);
  });

  test("yanlış adet orderLimits kg üründe yok sayılır", () => {
    const rule = resolveProductOrderRule({
      name: "Domates",
      categoryName: "Meyve Sebze",
      isWeightBased: true,
      orderLimits: {
        unit: "adet",
        isWeightBased: false,
        minQuantity: 1,
        maxQuantity: 5,
        step: 1,
      },
    });
    expect(rule.unit).toBe("kg");
    expect(rule.max_quantity).toBe(DEFAULT_KG_RULE.max_quantity);
  });

  test("clampQuantityToRule sınırlar içinde tutar", () => {
    const rule = { min_quantity: 1, max_quantity: 5, step: 1 };
    expect(clampQuantityToRule(99, rule)).toBe(5);
    expect(clampQuantityToRule(0, rule)).toBe(1);
  });
});

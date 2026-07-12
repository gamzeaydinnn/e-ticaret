import {
  CANCEL_MODE,
  getCancelMode,
  getOrderActions,
  isSameBusinessDay,
} from "../utils/orderCancelPolicy";

describe("orderCancelPolicy", () => {
  test("PickedUp öncesi durumlar gün farkı olmadan auto", () => {
    const oldOrder = {
      status: "Preparing",
      orderDate: "2020-01-01T10:00:00Z",
    };
    expect(getCancelMode(oldOrder)).toBe(CANCEL_MODE.AUTO);

    const actions = getOrderActions(oldOrder, { isAuthenticated: true });
    expect(actions.showCancel).toBe(true);
    expect(actions.cancelLabel).toBe("Siparişi İptal Et");
  });

  test("PickedUp sonrası whatsapp", () => {
    const order = {
      status: "PickedUp",
      orderDate: new Date().toISOString(),
    };
    expect(getCancelMode(order)).toBe(CANCEL_MODE.WHATSAPP);

    const actions = getOrderActions(order, { isAuthenticated: true });
    expect(actions.showCancel).toBe(false);
    expect(actions.whatsAppPrimary).toBe(true);
  });

  test("canCancel true backend bayrağı auto verir", () => {
    expect(
      getCancelMode({
        status: "Preparing",
        canCancel: true,
        orderDate: "2019-05-01T00:00:00Z",
      }),
    ).toBe(CANCEL_MODE.AUTO);
  });

  test("terminal durum none", () => {
    expect(getCancelMode({ status: "Cancelled" })).toBe(CANCEL_MODE.NONE);
    expect(getCancelMode({ status: "Refunded" })).toBe(CANCEL_MODE.NONE);
  });

  test("isSameBusinessDay Turkey timezone ile çalışır", () => {
    const now = new Date();
    expect(isSameBusinessDay(now.toISOString())).toBe(true);
    expect(isSameBusinessDay("2010-01-01T12:00:00Z")).toBe(false);
  });
});

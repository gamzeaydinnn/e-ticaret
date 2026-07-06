import addressService from "../services/addressService";
import api from "../services/api";

jest.mock("../services/api", () => ({
  __esModule: true,
  default: {
    get: jest.fn(),
    post: jest.fn(),
    put: jest.fn(),
    delete: jest.fn(),
  },
}));

describe("addressService", () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  test("getAll maps backend Street to address field", async () => {
    api.get.mockResolvedValueOnce({
      success: true,
      data: [
        {
          id: 1,
          title: "Ev",
          fullName: "Ali Veli",
          phone: "555",
          city: "Muğla",
          district: "Bodrum",
          street: "Gölköy Mah.",
          postalCode: "48400",
          isDefault: true,
        },
      ],
    });

    const items = await addressService.getAll();

    expect(api.get).toHaveBeenCalledWith("/api/address");
    expect(items).toHaveLength(1);
    expect(items[0].address).toBe("Gölköy Mah.");
    expect(items[0].street).toBeUndefined();
  });

  test("create sends street from form.address", async () => {
    api.post.mockResolvedValueOnce({
      success: true,
      data: { id: 2, street: "Yeni Sokak" },
    });

    const created = await addressService.create({
      title: "İş",
      fullName: "Ali",
      phone: "555",
      city: "Muğla",
      district: "Bodrum",
      address: "Yeni Sokak",
    });

    expect(api.post).toHaveBeenCalledWith("/api/address", {
      title: "İş",
      fullName: "Ali",
      phone: "555",
      city: "Muğla",
      district: "Bodrum",
      street: "Yeni Sokak",
      postalCode: null,
      isDefault: false,
    });
    expect(created.address).toBe("Yeni Sokak");
  });

  test("update sends street payload", async () => {
    api.put.mockResolvedValueOnce({
      success: true,
      data: { id: 1, street: "Güncel" },
    });

    await addressService.update(1, { address: "Güncel", title: "Ev" });

    expect(api.put).toHaveBeenCalledWith("/api/address/1", {
      title: "Ev",
      fullName: undefined,
      phone: undefined,
      city: undefined,
      district: undefined,
      street: "Güncel",
      postalCode: null,
      isDefault: false,
    });
  });

  test("remove calls delete endpoint", async () => {
    api.delete.mockResolvedValueOnce({ success: true });

    await addressService.remove(5);

    expect(api.delete).toHaveBeenCalledWith("/api/address/5");
  });
});

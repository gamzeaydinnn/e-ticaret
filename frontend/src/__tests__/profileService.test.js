import profileService from "../services/profileService";
import api from "../services/api";

jest.mock("../services/api", () => ({
  __esModule: true,
  default: {
    get: jest.fn(),
    put: jest.fn(),
    post: jest.fn(),
  },
}));

describe("profileService", () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  test("getProfile calls /api/profile", async () => {
    const payload = {
      success: true,
      data: { firstName: "Ali", lastName: "Veli", email: "a@b.com" },
    };
    api.get.mockResolvedValueOnce(payload);

    const result = await profileService.getProfile();

    expect(api.get).toHaveBeenCalledWith("/api/profile");
    expect(result).toEqual(payload);
  });

  test("updateProfile sends camelCase fields", async () => {
    api.put.mockResolvedValueOnce({ success: true, message: "Profil güncellendi" });

    await profileService.updateProfile({
      firstName: "Ali",
      lastName: "Veli",
      phoneNumber: "5551234567",
    });

    expect(api.put).toHaveBeenCalledWith("/api/profile", {
      firstName: "Ali",
      lastName: "Veli",
      phoneNumber: "5551234567",
    });
  });

  test("changePassword posts to /api/profile/change-password", async () => {
    api.post.mockResolvedValueOnce({ success: true, message: "Şifre değiştirildi" });

    await profileService.changePassword({
      oldPassword: "old123",
      newPassword: "new456",
      confirmPassword: "new456",
    });

    expect(api.post).toHaveBeenCalledWith("/api/profile/change-password", {
      oldPassword: "old123",
      newPassword: "new456",
      confirmPassword: "new456",
    });
  });
});

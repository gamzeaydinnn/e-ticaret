test("frontend audit services compile", () => {
  expect(require("./services/profileService").default).toBeDefined();
  expect(require("./services/addressService").default).toBeDefined();
});

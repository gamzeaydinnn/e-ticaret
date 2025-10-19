// Manual mock for axios to work with Jest in CRA
/* eslint-disable no-undef */
const mockAxiosInstance = {
  get: jest.fn(),
  post: jest.fn(),
  put: jest.fn(),
  delete: jest.fn(),
  patch: jest.fn(),
  interceptors: {
    request: { use: jest.fn() },
    response: { use: jest.fn() },
  },
};

module.exports = {
  create: jest.fn(() => mockAxiosInstance),
  __esModule: true,
  default: {
    create: jest.fn(() => mockAxiosInstance),
  },
};


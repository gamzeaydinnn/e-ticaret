// jest-dom adds custom jest matchers for asserting on DOM nodes.
// allows you to do things like:
// expect(element).toHaveTextContent(/react/i)
// learn more: https://github.com/testing-library/jest-dom
import '@testing-library/jest-dom';
import React from 'react';
// Mock axios globally to avoid ESM parsing issues in Jest
jest.mock('axios');

// Mock react-router-dom to avoid ESM issues and provide minimal stubs
jest.mock('react-router-dom', () => {
  const ReactMock = require('react');
  return {
    Link: ({ children, ...props }) => ReactMock.createElement('a', props, children),
    useLocation: () => ({ pathname: '/' }),
    useNavigate: () => () => {},
    BrowserRouter: ({ children }) => ReactMock.createElement('div', null, children),
    Routes: ({ children }) => ReactMock.createElement('div', null, children),
    Route: ({ element }) => element || null,
  };
});

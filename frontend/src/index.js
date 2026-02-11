import React from "react";
import ReactDOM from "react-dom/client";
import "./index.css";
import App from "./App";
import { HelmetProvider } from "react-helmet-async";
import reportWebVitals from "./reportWebVitals";

// GÜVENLİK: Production ortamında console.log'ları devre dışı bırak
// Hassas bilgi sızıntısını önler
if (process.env.NODE_ENV === "production") {
  const noop = () => {};
  // Sadece log ve debug metodlarını devre dışı bırak (warn ve error'ı hata takibi için tut)
  console.log = noop;
  console.debug = noop;
  console.info = noop;
}

const root = ReactDOM.createRoot(document.getElementById("root"));
root.render(
  <React.StrictMode>
    <HelmetProvider>
      <App />
    </HelmetProvider>
  </React.StrictMode>,
);

// If you want to start measuring performance in your app, pass a function
// to log results (for example: reportWebVitals(console.log))
// or send to an analytics endpoint. Learn more: https://bit.ly/CRA-vitals
reportWebVitals();

import React from "react";
import { ToastContainer, toast } from "react-toastify";
import "react-toastify/dist/ReactToastify.css";

// Basit global toast helper'ı; istediğinde import edip kullanabilirsin:
// toast.success("Mesaj"), toast.error("Hata") vb.

// Merkezi z-index ölçeği ile uyumlu: toast bildirimleri her zaman header/modal üstünde
// görünmeli (bkz. notification katmanı standardizasyonu).
const TOAST_Z_INDEX = 13000;

export function GlobalToastContainer() {
  return (
    <ToastContainer
      position="top-right"
      autoClose={4000}
      hideProgressBar={false}
      newestOnTop
      closeOnClick
      pauseOnFocusLoss
      draggable
      pauseOnHover
      theme="colored"
      style={{ zIndex: TOAST_Z_INDEX }}
    />
  );
}

export { toast };


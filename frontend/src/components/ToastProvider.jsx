import React from "react";
import { ToastContainer, toast } from "react-toastify";
import "react-toastify/dist/ReactToastify.css";

// Basit global toast helper'ı; istediğinde import edip kullanabilirsin:
// toast.success("Mesaj"), toast.error("Hata") vb.

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
    />
  );
}

export { toast };


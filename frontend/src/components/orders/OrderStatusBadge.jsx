import React from "react";
import { getStatusConfig } from "../../utils/orderStatusConfig";
import "./OrderActions.css";

export default function OrderStatusBadge({ status, className = "" }) {
  const config = getStatusConfig(status);
  return (
    <span
      className={`order-status-badge ${className}`}
      style={{
        backgroundColor: config.bgColor,
        color: config.color,
        border: `1px solid ${config.color}33`,
      }}
    >
      <i className={`fas ${config.icon} me-1`} />
      {config.shortLabel}
    </span>
  );
}

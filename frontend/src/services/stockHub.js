// Simple SignalR client wrapper for stock updates
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { getApiBaseUrl } from '../config/apiConfig';

let connection;
let listeners = new Set();

export function startStockHub() {
  if (connection) return connection;

  const baseUrl = getApiBaseUrl();
  connection = new HubConnectionBuilder()
    .withUrl(`${baseUrl}/hubs/stock`)
    .withAutomaticReconnect()
    .configureLogging(LogLevel.Error)
    .build();

  connection.on('StockUpdated', (payload) => {
    // Notify all listeners
    listeners.forEach((cb) => {
      try { cb(payload); } catch { /* ignore */ }
    });
  });

  connection.start().catch(() => {
    // retry automatically via withAutomaticReconnect
  });

  return connection;
}

export function subscribeStockUpdates(callback) {
  startStockHub();
  listeners.add(callback);
  return () => listeners.delete(callback);
}

export async function joinProduct(productId) {
  if (!connection) startStockHub();
  try {
    await connection.invoke('JoinProductGroup', String(productId));
  } catch {}
}

export async function leaveProduct(productId) {
  if (!connection) return;
  try {
    await connection.invoke('LeaveProductGroup', String(productId));
  } catch {}
}


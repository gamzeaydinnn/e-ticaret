/**
 * Müşteri destek / WhatsApp yardımcıları — siteSettingsService üzerinden merkezi.
 */
import { getContactInfo } from "../services/siteSettingsService";

let cachedContact = null;

async function loadContact() {
  if (cachedContact) return cachedContact;
  cachedContact = await getContactInfo();
  return cachedContact;
}

export async function getWhatsAppNumber() {
  const contact = await loadContact();
  return contact?.whatsAppNumber || contact?.whatsappNumber || "905334783072";
}

export async function getSupportPhoneDisplay() {
  const contact = await loadContact();
  return contact?.phoneDisplay || contact?.phone || "+90 533 478 30 72";
}

export function buildWhatsAppMessage(orderNumber, context = "destek") {
  if (context === "cancel") {
    return `Merhaba, ${orderNumber} numaralı siparişim için iptal/iade talebi oluşturmak istiyorum.`;
  }
  return `Merhaba, ${orderNumber} numaralı siparişim hakkında destek almak istiyorum.`;
}

export async function buildWhatsAppUrl(orderNumber, context = "destek") {
  const number = await getWhatsAppNumber();
  const message = buildWhatsAppMessage(orderNumber, context);
  return `https://wa.me/${number}?text=${encodeURIComponent(message)}`;
}

export function openWhatsAppSupport(orderNumber, context = "destek", whatsAppNumber = "905334783072") {
  const message = buildWhatsAppMessage(orderNumber, context);
  const url = `https://wa.me/${whatsAppNumber}?text=${encodeURIComponent(message)}`;
  window.open(url, "_blank", "noopener,noreferrer");
}

export async function openWhatsAppSupportAsync(orderNumber, context = "destek") {
  const url = await buildWhatsAppUrl(orderNumber, context);
  window.open(url, "_blank", "noopener,noreferrer");
}

export function clearSupportCache() {
  cachedContact = null;
}

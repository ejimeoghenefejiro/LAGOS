const allowed = ["WHATSAPP", "SMS", "IN_APP"];
const key = (merchantId: string) => `lgrrs.receipt-preferences.${merchantId}`;
export function loadDeliveryPreferences(merchantId?: string): string[] {
  try {
    const stored: unknown = JSON.parse(localStorage.getItem(key(merchantId ?? "")) ?? "null");
    if (Array.isArray(stored) && stored.length > 0 && stored.every(c => allowed.includes(c)))
      return [...new Set(stored as string[])];
  } catch { /* Use defaults when storage is unavailable. */ }
  return ["WHATSAPP", "SMS"];
}
export function saveDeliveryPreferences(merchantId: string, channels: string[]) {
  if (!channels.length || channels.some(c => !allowed.includes(c))) throw new Error("Choose at least one delivery channel.");
  localStorage.setItem(key(merchantId), JSON.stringify(channels));
}

import type { ComponentType } from "react";
import { IconSms, IconSmartphone, IconWhatsApp } from "./icons";

const CHANNEL_ICON: Record<string, { icon: ComponentType<{ className?: string }>; label: string; tone: string }> = {
  WHATSAPP: { icon: IconWhatsApp, label: "WhatsApp", tone: "whatsapp" },
  WhatsApp: { icon: IconWhatsApp, label: "WhatsApp", tone: "whatsapp" },
  SMS: { icon: IconSms, label: "SMS", tone: "sms" },
  Sms: { icon: IconSms, label: "SMS", tone: "sms" },
  IN_APP: { icon: IconSmartphone, label: "In-App", tone: "inapp" },
  InApp: { icon: IconSmartphone, label: "In-App", tone: "inapp" }
};

export default function DeliveryIcons({ channels }: { channels: string[] }) {
  return (
    <div className="delivery-icons">
      {channels.map((channel) => {
        const meta = CHANNEL_ICON[channel];
        if (!meta) return null;
        const Icon = meta.icon;
        return (
          <span key={channel} className={`delivery-icon tone-${meta.tone}`} title={meta.label}>
            <Icon />
          </span>
        );
      })}
    </div>
  );
}

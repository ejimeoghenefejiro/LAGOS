import { IconSms, IconSmartphone, IconWhatsApp } from "./icons";

const CHANNEL_META: Record<string, { icon: typeof IconWhatsApp; label: string; tone: string }> = {
  WHATSAPP: { icon: IconWhatsApp, label: "WhatsApp", tone: "whatsapp" },
  WhatsApp: { icon: IconWhatsApp, label: "WhatsApp", tone: "whatsapp" },
  SMS: { icon: IconSms, label: "SMS", tone: "sms" },
  Sms: { icon: IconSms, label: "SMS", tone: "sms" },
  IN_APP: { icon: IconSmartphone, label: "App", tone: "inapp" },
  InApp: { icon: IconSmartphone, label: "App", tone: "inapp" }
};

export default function ChannelButton({ channel }: { channel: string }) {
  const meta = CHANNEL_META[channel];
  if (!meta) return null;
  const Icon = meta.icon;
  return (
    <span className={`channel-button tone-${meta.tone}`}>
      <Icon width={16} height={16} />
      {meta.label}
    </span>
  );
}

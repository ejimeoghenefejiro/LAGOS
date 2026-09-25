import type { SVGProps } from "react";

type IconProps = SVGProps<SVGSVGElement>;

const base = {
  width: 20,
  height: 20,
  viewBox: "0 0 24 24",
  fill: "none",
  stroke: "currentColor",
  strokeWidth: 1.8,
  strokeLinecap: "round" as const,
  strokeLinejoin: "round" as const
};

const filledBase = {
  width: 20,
  height: 20
};

export function IconHome(props: IconProps) {
  return (
    <svg {...base} {...props}>
      <path d="M3 11.5 12 4l9 7.5" />
      <path d="M5 10v9a1 1 0 0 0 1 1h4v-6h4v6h4a1 1 0 0 0 1-1v-9" />
    </svg>
  );
}

export function IconReceiptPlus(props: IconProps) {
  return (
    <svg {...base} {...props}>
      <path d="M6 3h9l3 3v15l-3-2-3 2-3-2-3 2V3Z" />
      <path d="M9 9h6M9 13h3" />
    </svg>
  );
}

export function IconUsers(props: IconProps) {
  return (
    <svg {...base} {...props}>
      <circle cx="9" cy="8" r="3" />
      <path d="M3 20c0-3.3 2.7-6 6-6s6 2.7 6 6" />
      <circle cx="17" cy="9" r="2.5" />
      <path d="M15.5 14.2c2.4.4 4.5 2.5 4.5 5.8" />
    </svg>
  );
}

export function IconHistory(props: IconProps) {
  return (
    <svg {...base} {...props}>
      <circle cx="12" cy="13" r="8" />
      <path d="M12 9v4l3 2" />
      <path d="M9 2h6" />
    </svg>
  );
}

export function IconSettings(props: IconProps) {
  return (
    <svg {...base} {...props}>
      <circle cx="12" cy="12" r="3" />
      <path d="M19.4 13a7.6 7.6 0 0 0 0-2l2-1.6-2-3.4-2.4.7a7.7 7.7 0 0 0-1.8-1L14.7 3h-4l-.5 2.7a7.7 7.7 0 0 0-1.8 1l-2.4-.7-2 3.4L5.6 11a7.6 7.6 0 0 0 0 2l-2 1.6 2 3.4 2.4-.7c.5.4 1.1.8 1.8 1l.5 2.7h4l.5-2.7c.7-.2 1.3-.6 1.8-1l2.4.7 2-3.4-2-1.6Z" />
    </svg>
  );
}

export function IconMenu(props: IconProps) {
  return (
    <svg {...base} {...props}>
      <path d="M4 6h16M4 12h16M4 18h16" />
    </svg>
  );
}

export function IconBell(props: IconProps) {
  return (
    <svg {...base} {...props}>
      <path d="M6 9a6 6 0 0 1 12 0c0 4 1.5 5.5 1.5 5.5H4.5S6 13 6 9Z" />
      <path d="M10 19a2 2 0 0 0 4 0" />
    </svg>
  );
}

export function IconChevronDown(props: IconProps) {
  return (
    <svg {...base} {...props}>
      <path d="m6 9 6 6 6-6" />
    </svg>
  );
}

export function IconShieldCheck(props: IconProps) {
  return (
    <svg {...base} {...props}>
      <path d="M12 3 5 6v6c0 4.5 3 7.5 7 9 4-1.5 7-4.5 7-9V6l-7-3Z" />
      <path d="m9 12 2 2 4-4" />
    </svg>
  );
}

export function IconWhatsApp(props: IconProps) {
  return (
    <svg {...filledBase} viewBox="0 0 24 24" fill="currentColor" {...props}>
      <path d="M12 2a10 10 0 0 0-8.6 15.1L2 22l5-1.4A10 10 0 1 0 12 2Zm5.6 14.3c-.2.7-1.4 1.3-2 1.4-.5.1-1.2.2-3.7-.8-3.1-1.3-5.1-4.5-5.3-4.7-.1-.2-1.3-1.7-1.3-3.3 0-1.6.8-2.3 1.1-2.6.3-.3.6-.4.8-.4h.6c.2 0 .5 0 .7.5l1 2.4c.1.2.1.4 0 .6l-.5.7c-.1.2-.2.4 0 .6.3.5 1 1.3 1.9 2 .9.8 1.7 1.1 2 1.2.2.1.4.1.6-.1l.7-.8c.2-.3.4-.2.7-.1l2.1 1c.2.1.4.2.5.3.1.2.1.9-.1 1.6Z" />
    </svg>
  );
}

export function IconSms(props: IconProps) {
  return (
    <svg {...base} {...props}>
      <path d="M4 5h16v11H9l-4 3v-3H4Z" />
      <path d="M8 9.5h8M8 12.5h5" />
    </svg>
  );
}

export function IconSmartphone(props: IconProps) {
  return (
    <svg {...base} {...props}>
      <rect x="7" y="2.5" width="10" height="19" rx="2" />
      <path d="M11 18h2" />
    </svg>
  );
}

export function IconSend(props: IconProps) {
  return (
    <svg {...base} {...props}>
      <path d="M4 12 21 4l-7 17-3-6-7-3Z" />
    </svg>
  );
}

export function IconMapPin(props: IconProps) {
  return (
    <svg {...base} {...props}>
      <path d="M12 21s7-6.5 7-11.5A7 7 0 0 0 5 9.5C5 14.5 12 21 12 21Z" />
      <circle cx="12" cy="9.5" r="2.3" />
    </svg>
  );
}

export function IconLock(props: IconProps) {
  return (
    <svg {...base} {...props}>
      <rect x="5" y="10.5" width="14" height="9" rx="2" />
      <path d="M8 10.5V7a4 4 0 0 1 8 0v3.5" />
    </svg>
  );
}

export function IconStore(props: IconProps) {
  return (
    <svg {...base} {...props}>
      <path d="M4 8 5.5 4h13L20 8" />
      <path d="M4 8v11a1 1 0 0 0 1 1h14a1 1 0 0 0 1-1V8" />
      <path d="M4 8a3 3 0 0 0 6 0 3 3 0 0 0 6 0 3 3 0 0 0 6 0" />
    </svg>
  );
}

export function IconPhone(props: IconProps) {
  return (
    <svg {...base} {...props}>
      <path d="M6 3h3l1.5 4-2 1.5a12 12 0 0 0 6 6L16 12.5l4 1.5v3a2 2 0 0 1-2 2C10.5 19 5 13.5 5 5a2 2 0 0 1 1-2Z" />
    </svg>
  );
}

export function IconMail(props: IconProps) {
  return (
    <svg {...base} {...props}>
      <rect x="3" y="5" width="18" height="14" rx="2" />
      <path d="m4 6.5 8 6 8-6" />
    </svg>
  );
}

export function IconMoreVertical(props: IconProps) {
  return (
    <svg {...filledBase} viewBox="0 0 24 24" fill="currentColor" {...props}>
      <circle cx="12" cy="5" r="1.6" />
      <circle cx="12" cy="12" r="1.6" />
      <circle cx="12" cy="19" r="1.6" />
    </svg>
  );
}

export function IconHexagon(props: IconProps) {
  return (
    <svg {...filledBase} viewBox="0 0 24 24" fill="currentColor" {...props}>
      <path d="M12 2 21 7v10l-9 5-9-5V7l9-5Z" opacity="0.15" />
      <path d="M12 2 21 7v10l-9 5-9-5V7l9-5Z" fill="none" stroke="currentColor" strokeWidth="1.6" />
      <path d="m8.5 12 2.3 2.3L15.5 9.5" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  );
}

export function IconTicket(props: IconProps) {
  return (
    <svg {...base} {...props}>
      <path d="M3 8a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2v2a1.6 1.6 0 0 0 0 3.2V15a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-1.8a1.6 1.6 0 0 0 0-3.2V8Z" />
      <path d="M14 6v12" strokeDasharray="2.5 2.5" />
    </svg>
  );
}

export function IconScan(props: IconProps) {
  return (
    <svg {...base} {...props}>
      <path d="M4 8V6a2 2 0 0 1 2-2h2M4 16v2a2 2 0 0 0 2 2h2M20 8V6a2 2 0 0 0-2-2h-2M20 16v2a2 2 0 0 1-2 2h-2" />
      <path d="M4 12h16" strokeDasharray="1.6 2" />
    </svg>
  );
}

export function IconInfoCircle(props: IconProps) {
  return (
    <svg {...base} {...props}>
      <circle cx="12" cy="12" r="9" />
      <path d="M12 11v5.5" />
      <circle cx="12" cy="8" r="0.6" fill="currentColor" stroke="none" />
    </svg>
  );
}

export function IconCalendar(props: IconProps) {
  return (
    <svg {...base} {...props}>
      <rect x="4" y="5.5" width="16" height="15" rx="2" />
      <path d="M4 10h16M8 3.5v3.5M16 3.5v3.5" />
    </svg>
  );
}

export function IconCoin(props: IconProps) {
  return (
    <svg {...base} {...props}>
      <circle cx="12" cy="12" r="8.5" />
      <path d="M12 8v8M9.5 9.5a2.2 2.2 0 0 1 2.5-1.5c1.4 0 2.5.8 2.5 1.8s-1 1.4-2.5 1.7c-1.5.3-2.5.8-2.5 1.8s1.1 1.7 2.5 1.7a2.3 2.3 0 0 0 2.5-1.5" />
    </svg>
  );
}

export function IconTrophy(props: IconProps) {
  return (
    <svg {...base} {...props}>
      <path d="M7 4h10v5a5 5 0 0 1-10 0V4Z" />
      <path d="M7 5H4v1.5A3.5 3.5 0 0 0 7 10M17 5h3v1.5A3.5 3.5 0 0 1 17 10" />
      <path d="M10 15.5v2M14 15.5v2M8.5 20h7l-1-2.5h-5L8.5 20Z" />
    </svg>
  );
}

export function IconGift(props: IconProps) {
  return (
    <svg {...base} {...props}>
      <rect x="4" y="9" width="16" height="4" rx="0.6" />
      <rect x="5.5" y="13" width="13" height="8" rx="0.6" />
      <path d="M12 9v12" />
      <path d="M12 9C10 6 7 6 7 8.2 7 9.4 8.5 9 12 9ZM12 9c2-3 5-3 5-.8 0 1.2-1.5.8-5 .8Z" />
    </svg>
  );
}

export function IconUserCircle(props: IconProps) {
  return (
    <svg {...base} {...props}>
      <circle cx="12" cy="9.5" r="3" />
      <path d="M5.5 19a6.7 6.7 0 0 1 13 0" />
    </svg>
  );
}

export function IconClipboardList(props: IconProps) {
  return (
    <svg {...base} {...props}>
      <rect x="5" y="4" width="14" height="18" rx="2" />
      <rect x="9" y="2.5" width="6" height="3" rx="1" />
      <path d="M8.5 11h7M8.5 14.5h7M8.5 18h4.5" />
    </svg>
  );
}

export function IconBarChart(props: IconProps) {
  return (
    <svg {...base} {...props}>
      <path d="M5 20V10M12 20V4M19 20v-7" />
      <path d="M3 20h18" />
    </svg>
  );
}

export function IconCalendarRange(props: IconProps) {
  return (
    <svg {...base} {...props}>
      <rect x="3.5" y="5.5" width="17" height="15" rx="2" />
      <path d="M3.5 10h17M8 3.5v3.5M16 3.5v3.5" />
      <path d="M8 14h2M14 14h2M8 17h2M14 17h2" />
    </svg>
  );
}

export function IconCheckCircleFilled(props: IconProps) {
  return (
    <svg {...filledBase} viewBox="0 0 24 24" fill="currentColor" {...props}>
      <circle cx="12" cy="12" r="10" opacity="0.18" />
      <path d="M8.2 12.3 11 15l5-6" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  );
}

export const isNigerianPhone = (phone: string) => /^0[789][0-9]{9}$/.test(phone);
export const phoneHint = "Enter all 11 digits, starting with 07, 08 or 09 (for example 08139662026).";

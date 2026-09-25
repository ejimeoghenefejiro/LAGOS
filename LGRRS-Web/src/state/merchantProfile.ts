import { createContext, useContext } from "react";
import type { MerchantSummary } from "../api/types";

interface MerchantProfileContextValue {
  profile: MerchantSummary | null;
}

export const MerchantProfileContext = createContext<MerchantProfileContextValue>({ profile: null });

export const useMerchantProfile = () => useContext(MerchantProfileContext);

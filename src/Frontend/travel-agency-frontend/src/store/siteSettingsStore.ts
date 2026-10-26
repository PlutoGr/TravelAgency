import { useEffect } from 'react';
import { create } from 'zustand';
import { getPublicSiteSettings } from '@/api/siteSettings';
import {
  DEFAULT_SITE_NAME,
  EMPTY_SITE_SETTINGS,
  type SiteSettingsPublic,
} from '@/domain/siteSettings';

interface SiteSettingsState {
  settings: SiteSettingsPublic;
  setSettings: (settings: SiteSettingsPublic) => void;
  reset: () => void;
}

export const useSiteSettingsStore = create<SiteSettingsState>((set) => ({
  settings: EMPTY_SITE_SETTINGS,
  setSettings: (settings) =>
    set({
      settings: {
        ...settings,
        siteName: settings.siteName?.trim() || DEFAULT_SITE_NAME,
        socialLinks: settings.socialLinks ?? [],
      },
    }),
  reset: () => set({ settings: EMPTY_SITE_SETTINGS }),
}));

let loadStarted = false;

export function ensurePublicSiteSettings(): void {
  if (loadStarted || import.meta.env.MODE === 'test' || import.meta.env.VITEST) return;
  loadStarted = true;
  void getPublicSiteSettings()
    .then((settings) => useSiteSettingsStore.getState().setSettings(settings))
    .catch(() => {
      loadStarted = false;
    });
}

export function usePublicSiteSettings(): SiteSettingsPublic {
  const settings = useSiteSettingsStore((state) => state.settings);
  useEffect(() => {
    ensurePublicSiteSettings();
  }, []);
  return settings;
}

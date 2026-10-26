export interface SocialLink {
  label: string;
  url: string;
}

export interface SiteSettingsPublic {
  siteName: string;
  supportPhone: string | null;
  contactEmail: string | null;
  officeAddress: string | null;
  workingHours: string | null;
  socialLinks: SocialLink[];
  bannerText: string | null;
  bannerEnabled: boolean;
}

export interface SiteSettingsAdmin extends SiteSettingsPublic {
  updatedAt: string;
  updatedByUserId: string | null;
  message?: string | null;
}

export interface SiteSettingsAuditItem {
  fieldName: string;
  oldValue: string | null;
  newValue: string | null;
  changedByUserId: string;
  changedAt: string;
}

export interface UpdateSiteSettingsPayload {
  siteName: string;
  supportPhone: string | null;
  contactEmail: string | null;
  officeAddress: string | null;
  workingHours: string | null;
  socialLinks: SocialLink[];
  bannerText: string | null;
  bannerEnabled: boolean;
}

export const SITE_SETTINGS_SAVED_MESSAGE =
  'Настройки сохранены. Изменения на сайте появятся в течение минуты.';

export const DEFAULT_SITE_NAME = 'TravelAgency';
export const FOOTER_PHONE_PLACEHOLDER = '+7 999 999-99-99';
export const LATER_LABEL = 'Появится позже';

export const EMPTY_SITE_SETTINGS: SiteSettingsPublic = {
  siteName: DEFAULT_SITE_NAME,
  supportPhone: null,
  contactEmail: null,
  officeAddress: null,
  workingHours: null,
  socialLinks: [],
  bannerText: null,
  bannerEnabled: false,
};

const E164 = /^\+[1-9]\d{1,14}$/;
const EMAIL = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

export function isE164Phone(value: string): boolean {
  return E164.test(value);
}

export function isContactEmail(value: string): boolean {
  return EMAIL.test(value);
}

export function isHttpsUrl(value: string): boolean {
  return value.startsWith('https://') && value.length > 'https://'.length;
}

export const AUDIT_FIELD_LABELS: Record<string, string> = {
  SiteName: 'Название сайта',
  SupportPhone: 'Телефон поддержки',
  ContactEmail: 'Email для связи',
  OfficeAddress: 'Адрес офиса',
  WorkingHours: 'Часы работы',
  SocialLinksJson: 'Ссылки',
  BannerText: 'Текст баннера',
  BannerEnabled: 'Баннер включён',
};

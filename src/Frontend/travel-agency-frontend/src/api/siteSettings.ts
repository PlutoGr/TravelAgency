import axios from 'axios';
import type {
  SiteSettingsAdmin,
  SiteSettingsAuditItem,
  SiteSettingsPublic,
  UpdateSiteSettingsPayload,
} from '@/domain/siteSettings';
import { apiClient } from './client';

export async function getPublicSiteSettings(): Promise<SiteSettingsPublic> {
  const { data } = await apiClient.get<SiteSettingsPublic>('/site-settings');
  return data;
}

export async function getAdminSiteSettings(): Promise<SiteSettingsAdmin> {
  const { data } = await apiClient.get<SiteSettingsAdmin>('/site-settings/manage');
  return data;
}

export async function updateSiteSettings(
  payload: UpdateSiteSettingsPayload,
): Promise<SiteSettingsAdmin> {
  const { data } = await apiClient.put<SiteSettingsAdmin>('/site-settings/manage', payload);
  return data;
}

export async function getSiteSettingsAudit(): Promise<SiteSettingsAuditItem[]> {
  const { data } = await apiClient.get<SiteSettingsAuditItem[]>('/site-settings/manage/audit');
  return data;
}

export function siteSettingsFieldErrors(error: unknown): Record<string, string> {
  if (!axios.isAxiosError(error)) return {};
  const errors = error.response?.data?.errors;
  if (!errors || typeof errors !== 'object') return {};

  const result: Record<string, string> = {};
  for (const [key, value] of Object.entries(errors as Record<string, unknown>)) {
    const text = Array.isArray(value) ? String(value[0] ?? '') : String(value ?? '');
    if (text) result[key] = text;
  }
  return result;
}

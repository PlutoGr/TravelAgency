import { beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import toast from 'react-hot-toast';
import AdminSiteSettingsPage from './AdminSiteSettingsPage';
import { SITE_SETTINGS_SAVED_MESSAGE } from '@/domain/siteSettings';
import { useSiteSettingsStore } from '@/store/siteSettingsStore';

vi.mock('react-hot-toast', () => ({
  default: { error: vi.fn(), success: vi.fn() },
}));

const getAdminSiteSettings = vi.fn();
const getSiteSettingsAudit = vi.fn();
const updateSiteSettings = vi.fn();

vi.mock('@/api/siteSettings', () => ({
  getAdminSiteSettings: (...args: unknown[]) => getAdminSiteSettings(...args),
  getSiteSettingsAudit: (...args: unknown[]) => getSiteSettingsAudit(...args),
  updateSiteSettings: (...args: unknown[]) => updateSiteSettings(...args),
  siteSettingsFieldErrors: (error: unknown) => {
    const data = (error as { response?: { data?: { errors?: Record<string, string[]> } } }).response?.data
      ?.errors;
    if (!data) return {};
    return Object.fromEntries(Object.entries(data).map(([key, messages]) => [key, messages[0]]));
  },
}));

const savedAt = '2026-10-26T12:00:00.000Z';
const adminId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';

function loadedSettings(overrides: Record<string, unknown> = {}) {
  return {
    siteName: 'TravelAgency',
    supportPhone: null,
    contactEmail: null,
    officeAddress: null,
    workingHours: null,
    socialLinks: [],
    bannerText: null,
    bannerEnabled: false,
    updatedAt: savedAt,
    updatedByUserId: null,
    ...overrides,
  };
}

describe('AdminSiteSettingsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    useSiteSettingsStore.getState().reset();
    getAdminSiteSettings.mockResolvedValue(loadedSettings());
    getSiteSettingsAudit.mockResolvedValue([]);
  });

  it('shows the settings fields and an empty journal', async () => {
    render(<AdminSiteSettingsPage />);

    await waitFor(() => {
      expect(screen.getByLabelText('Название сайта')).toHaveValue('TravelAgency');
    });
    expect(screen.getByLabelText('Телефон поддержки')).toBeInTheDocument();
    expect(screen.getByLabelText('Email для связи')).toBeInTheDocument();
    expect(screen.getByLabelText('Адрес офиса')).toBeInTheDocument();
    expect(screen.getByLabelText('Часы работы')).toBeInTheDocument();
    expect(screen.getByLabelText('Текст баннера')).toBeInTheDocument();
    expect(screen.getByRole('checkbox', { name: 'Показывать баннер' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Сохранить' })).toBeEnabled();
    expect(screen.getByRole('region', { name: 'Журнал изменений' })).toHaveTextContent('Изменений пока нет.');
  });

  it('saves a phone and shows the cache notice', async () => {
    const user = userEvent.setup();
    updateSiteSettings.mockResolvedValue(
      loadedSettings({
        supportPhone: '+79991234567',
        message: SITE_SETTINGS_SAVED_MESSAGE,
        updatedByUserId: adminId,
      }),
    );
    getSiteSettingsAudit
      .mockResolvedValueOnce([])
      .mockResolvedValueOnce([
        {
          fieldName: 'SupportPhone',
          oldValue: null,
          newValue: '+79991234567',
          changedByUserId: adminId,
          changedAt: savedAt,
        },
      ]);

    render(<AdminSiteSettingsPage />);
    await waitFor(() => {
      expect(screen.getByRole('button', { name: 'Сохранить' })).toBeEnabled();
    });
    const phone = screen.getByLabelText('Телефон поддержки');
    await user.type(phone, '+79991234567');
    await user.click(screen.getByRole('button', { name: 'Сохранить' }));

    await waitFor(() => {
      expect(toast.success).toHaveBeenCalledWith(SITE_SETTINGS_SAVED_MESSAGE);
    });
    expect(updateSiteSettings).toHaveBeenCalledWith(
      expect.objectContaining({ supportPhone: '+79991234567', siteName: 'TravelAgency' }),
    );
    const journal = await screen.findByRole('region', { name: 'Журнал изменений' });
    expect(within(journal).getByText('Телефон поддержки')).toBeInTheDocument();
    expect(within(journal).getByText('+79991234567')).toBeInTheDocument();
    expect(within(journal).getByText(adminId)).toBeInTheDocument();
  });

  it('shows a field error and does not toast success when the phone is rejected', async () => {
    const user = userEvent.setup();
    updateSiteSettings.mockRejectedValue({
      response: { data: { errors: { supportPhone: ['Телефон должен быть в формате E.164, например +79991234567.'] } } },
    });

    render(<AdminSiteSettingsPage />);
    await waitFor(() => {
      expect(screen.getByRole('button', { name: 'Сохранить' })).toBeEnabled();
    });
    await user.click(screen.getByRole('button', { name: 'Сохранить' }));

    expect(await screen.findByText(/формате E\.164/)).toBeInTheDocument();
    expect(toast.success).not.toHaveBeenCalled();
  });
});

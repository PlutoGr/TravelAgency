import { beforeEach, describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import Header from './Header';
import Footer from './Footer';
import PublicLayout from './PublicLayout';
import SiteBanner from './SiteBanner';
import { useSiteSettingsStore } from '@/store/siteSettingsStore';
import { EMPTY_SITE_SETTINGS } from '@/domain/siteSettings';

describe('public site settings', () => {
  beforeEach(() => {
    useSiteSettingsStore.getState().reset();
  });

  it('renders the banner as text and does not interpret HTML', () => {
    const payload = '<img src=x onerror=alert(1)><script>alert(1)</script>';
    const { container } = render(<SiteBanner enabled text={payload} />);

    expect(screen.getByRole('status', { name: 'Объявление' })).toHaveTextContent(payload);
    expect(container.querySelector('script')).toBeNull();
    expect(container.querySelector('img')).toBeNull();
  });

  it('hides the banner when it is disabled', () => {
    render(<SiteBanner enabled={false} text="<script>alert(1)</script>" />);
    expect(screen.queryByRole('status', { name: 'Объявление' })).not.toBeInTheDocument();
  });

  it('shows the saved name, phone and contacts on the public shell', () => {
    useSiteSettingsStore.getState().setSettings({
      ...EMPTY_SITE_SETTINGS,
      siteName: 'Моретур',
      supportPhone: '+79991234567',
      contactEmail: 'user@example.com',
      officeAddress: 'Москва, ул. Примерная, 1',
      workingHours: 'ежедневно 10:00-19:00',
      socialLinks: [{ label: 'MAX', url: 'https://example.com/max' }],
      bannerText: '<script>alert(1)</script>',
      bannerEnabled: true,
    });

    render(
      <MemoryRouter>
        <Routes>
          <Route element={<PublicLayout />}>
            <Route index element={<div>Главная</div>} />
          </Route>
        </Routes>
      </MemoryRouter>,
    );

    expect(screen.getAllByText('Моретур').length).toBeGreaterThan(0);
    expect(screen.getAllByText('+79991234567').length).toBeGreaterThan(0);
    expect(screen.getByText('user@example.com')).toBeInTheDocument();
    expect(screen.getByText('Москва, ул. Примерная, 1')).toBeInTheDocument();
    expect(screen.getByText('ежедневно 10:00-19:00')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'MAX' })).toHaveAttribute('href', 'https://example.com/max');
    const banner = screen.getByRole('status', { name: 'Объявление' });
    expect(banner).toHaveTextContent('<script>alert(1)</script>');
    expect(banner.querySelector('script')).toBeNull();
    expect(screen.queryByText('+7 999 999-99-99')).not.toBeInTheDocument();
  });

  it('keeps the header logo and footer placeholders when settings are empty', () => {
    render(
      <MemoryRouter>
        <Header />
        <Footer />
      </MemoryRouter>,
    );

    expect(screen.getAllByText('TravelAgency').length).toBeGreaterThan(0);
    expect(screen.getByText('+7 999 999-99-99')).toBeInTheDocument();
    expect(screen.getAllByText('Появится позже').length).toBeGreaterThan(0);
    expect(screen.queryByRole('status', { name: 'Объявление' })).not.toBeInTheDocument();
  });
});
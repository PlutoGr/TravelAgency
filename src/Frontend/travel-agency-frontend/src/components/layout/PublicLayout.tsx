import Header from './Header';
import Footer from './Footer';
import RoutedOutlet from './RoutedOutlet';
import SiteBanner from './SiteBanner';
import { usePublicSiteSettings } from '@/store/siteSettingsStore';

export default function PublicLayout() {
  const settings = usePublicSiteSettings();

  return (
    <div className="flex min-h-screen flex-col">
      <Header />
      <div className="pt-16 lg:pt-20">
        <SiteBanner text={settings.bannerText} enabled={settings.bannerEnabled} />
      </div>
      <main className="flex-1">
        <RoutedOutlet />
      </main>
      <Footer />
    </div>
  );
}

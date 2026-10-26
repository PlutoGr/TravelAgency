import type { LucideIcon } from 'lucide-react';
import { Link } from 'react-router-dom';
import { Send, Phone, Mail, MapPin, Clock } from 'lucide-react';
import {
  FOOTER_PHONE_PLACEHOLDER,
  LATER_LABEL,
  isContactEmail,
  isE164Phone,
  isHttpsUrl,
} from '@/domain/siteSettings';
import { usePublicSiteSettings } from '@/store/siteSettingsStore';

const LATER = LATER_LABEL;

const TOURIST_LINKS = [
  { label: 'Популярные туры', href: '/tours' },
  { label: 'Отзывы', href: '/#reviews' },
] as const;

interface ContactItem {
  id: string;
  icon: LucideIcon;
  text: string;
  withLater: boolean;
  href?: string;
}

export default function Footer() {
  const settings = usePublicSiteSettings();
  const phone = settings.supportPhone?.trim() ?? '';
  const email = settings.contactEmail?.trim() ?? '';
  const address = settings.officeAddress?.trim() ?? '';
  const hours = settings.workingHours?.trim() ?? '';
  const socialLinks = (settings.socialLinks ?? []).filter(
    (link) => link.label.trim() && isHttpsUrl(link.url.trim()),
  );

  const contacts: ContactItem[] = [
    phone
      ? {
          id: 'phone',
          icon: Phone,
          text: phone,
          withLater: false,
          href: isE164Phone(phone) ? `tel:${phone}` : undefined,
        }
      : { id: 'phone', icon: Phone, text: FOOTER_PHONE_PLACEHOLDER, withLater: true },
    email
      ? {
          id: 'email',
          icon: Mail,
          text: email,
          withLater: false,
          href: isContactEmail(email) ? `mailto:${email}` : undefined,
        }
      : { id: 'email', icon: Mail, text: LATER, withLater: false },
    address
      ? { id: 'address', icon: MapPin, text: address, withLater: false }
      : { id: 'address', icon: MapPin, text: LATER, withLater: false },
  ];

  if (hours) {
    contacts.push({ id: 'hours', icon: Clock, text: hours, withLater: false });
  }

  return (
    <footer id="contacts" className="bg-dark text-white/80">
      <div className="mx-auto max-w-7xl px-4 py-16 lg:px-8">
        <div className="grid gap-12 md:grid-cols-2 lg:grid-cols-3">
          {/* Brand */}
          <div className="lg:col-span-1">
            <Link
              to="/"
              className="font-heading text-2xl font-bold text-white"
            >
              {settings.siteName}
            </Link>
            <p className="mt-4 text-sm leading-relaxed text-white/60">
              Откройте мир незабываемых путешествий. Мы создаём уникальные туры,
              которые превращают мечты в реальность.
            </p>
          </div>

          {/* For Tourists */}
          <div>
            <h3 className="mb-4 font-heading text-sm font-semibold uppercase tracking-wider text-white">
              Туристам
            </h3>
            <ul className="space-y-3">
              {TOURIST_LINKS.map((link) => (
                <li key={link.href}>
                  <Link
                    to={link.href}
                    className="text-sm text-white/60 transition-colors hover:text-terracotta"
                  >
                    {link.label}
                  </Link>
                </li>
              ))}
            </ul>
          </div>

          {/* Contacts */}
          <div>
            <h3 className="mb-4 font-heading text-sm font-semibold uppercase tracking-wider text-white">
              Контакты
            </h3>
            <ul className="space-y-3">
              {contacts.map(({ id, icon: Icon, text, withLater, href }) => (
                <li key={id} className="flex items-center gap-3 text-sm text-white/60">
                  <Icon size={16} className="shrink-0 text-terracotta" />
                  <span className="flex flex-wrap items-baseline gap-x-2">
                    {href ? (
                      <a href={href} className="transition-colors hover:text-terracotta">
                        {text}
                      </a>
                    ) : (
                      <span>{text}</span>
                    )}
                    {withLater ? <span>{LATER}</span> : null}
                  </span>
                </li>
              ))}
              {socialLinks.map((link) => (
                <li key={`${link.label}-${link.url}`}>
                  <a
                    href={link.url}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="text-sm text-white/60 transition-colors hover:text-terracotta"
                  >
                    {link.label}
                  </a>
                </li>
              ))}
            </ul>
          </div>
        </div>

        {/* Newsletter */}
        <div className="mt-12 rounded-2xl bg-dark-light p-6 md:flex md:items-center md:justify-between md:p-8">
          <div className="mb-4 md:mb-0">
            <h3 className="font-heading text-lg font-semibold text-white">
              Подпишитесь на рассылку
            </h3>
            <p className="mt-1 text-sm text-white/60">
              Получайте лучшие предложения первыми
            </p>
            <p className="mt-1 text-sm text-white/60">{LATER}</p>
          </div>
          <form
            onSubmit={(event) => event.preventDefault()}
            className="flex gap-3"
          >
            <div className="min-w-0 flex-1">
              <input
                type="email"
                disabled
                placeholder="Ваш email"
                className="h-11 w-full min-w-[200px] rounded-xl border border-white/10 bg-white/5 px-4 text-sm text-white placeholder-white/40 outline-none disabled:cursor-not-allowed disabled:opacity-50"
              />
            </div>
            <button
              type="submit"
              disabled
              className="flex h-11 items-center gap-2 rounded-xl bg-terracotta px-5 font-heading text-sm font-semibold text-white disabled:cursor-not-allowed disabled:opacity-50"
            >
              <Send size={16} />
              <span className="hidden sm:inline">Подписаться</span>
            </button>
          </form>
        </div>

        {/* Bottom */}
        <div className="mt-12 flex flex-col items-center justify-between gap-4 border-t border-white/10 pt-8 md:flex-row">
          <p className="text-sm text-white/40">
            © {new Date().getFullYear()} {settings.siteName}. Все права защищены.
          </p>
        </div>
      </div>
    </footer>
  );
}

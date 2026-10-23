import { Link } from 'react-router-dom';
import { Send, Phone, Mail, MapPin } from 'lucide-react';

const LATER = 'Появится позже';

const TOURIST_LINKS = [
  { label: 'Популярные туры', href: '/tours' },
  { label: 'Отзывы', href: '/#reviews' },
] as const;

const CONTACTS = [
  { id: 'phone', icon: Phone, text: '+7 999 999-99-99', withLater: true },
  { id: 'email', icon: Mail, text: LATER, withLater: false },
  { id: 'address', icon: MapPin, text: LATER, withLater: false },
] as const;

export default function Footer() {
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
              TravelAgency
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
              {CONTACTS.map(({ id, icon: Icon, text, withLater }) => (
                <li key={id} className="flex items-center gap-3 text-sm text-white/60">
                  <Icon size={16} className="shrink-0 text-terracotta" />
                  <span className="flex flex-wrap items-baseline gap-x-2">
                    <span>{text}</span>
                    {withLater ? <span>{LATER}</span> : null}
                  </span>
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
            © {new Date().getFullYear()} TravelAgency. Все права защищены.
          </p>
        </div>
      </div>
    </footer>
  );
}

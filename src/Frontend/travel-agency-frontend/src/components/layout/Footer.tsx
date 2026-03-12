import { useState } from 'react';
import { Link } from 'react-router-dom';
import { Send, Phone, Mail, MapPin } from 'lucide-react';
import { motion } from 'framer-motion';
import clsx from 'clsx';
import toast from 'react-hot-toast';

const COMPANY_LINKS = [
  { label: 'О нас', href: '/about' },
  { label: 'Блог', href: '/blog' },
  { label: 'Карьера', href: '/careers' },
] as const;

const TOURIST_LINKS = [
  { label: 'Популярные туры', href: '/tours' },
  { label: 'Направления', href: '/tours#destinations' },
  { label: 'Отзывы', href: '/#reviews' },
  { label: 'FAQ', href: '/faq' },
] as const;

const CONTACTS = [
  { icon: Phone, text: '+7 (800) 555-35-35' },
  { icon: Mail, text: 'info@travelagency.ru' },
  { icon: MapPin, text: 'Москва, ул. Тверская 1' },
] as const;

export default function Footer() {
  const [email, setEmail] = useState('');

  function handleSubscribe(e: React.FormEvent) {
    e.preventDefault();
    if (!email.trim()) return;
    toast.success('Спасибо за подписку!');
    setEmail('');
  }

  return (
    <footer id="contacts" className="bg-dark text-white/80">
      <div className="mx-auto max-w-7xl px-4 py-16 lg:px-8">
        <div className="grid gap-12 md:grid-cols-2 lg:grid-cols-4">
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

          {/* Company */}
          <div>
            <h3 className="mb-4 font-heading text-sm font-semibold uppercase tracking-wider text-white">
              Компания
            </h3>
            <ul className="space-y-3">
              {COMPANY_LINKS.map((link) => (
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
              {CONTACTS.map(({ icon: Icon, text }) => (
                <li key={text} className="flex items-center gap-3 text-sm text-white/60">
                  <Icon size={16} className="shrink-0 text-terracotta" />
                  {text}
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
          </div>
          <form
            onSubmit={handleSubscribe}
            className="flex gap-3"
          >
            <div className="min-w-0 flex-1">
              <input
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="Ваш email"
                className="h-11 w-full min-w-[200px] rounded-xl border border-white/10 bg-white/5 px-4 text-sm text-white placeholder-white/40 outline-none transition-colors focus:border-terracotta"
              />
            </div>
            <motion.button
              whileHover={{ scale: 1.03 }}
              whileTap={{ scale: 0.97 }}
              type="submit"
              className="flex h-11 items-center gap-2 rounded-xl bg-terracotta px-5 font-heading text-sm font-semibold text-white transition-colors hover:bg-terracotta-light"
            >
              <Send size={16} />
              <span className="hidden sm:inline">Подписаться</span>
            </motion.button>
          </form>
        </div>

        {/* Bottom */}
        <div className="mt-12 flex flex-col items-center justify-between gap-4 border-t border-white/10 pt-8 md:flex-row">
          <p className="text-sm text-white/40">
            © {new Date().getFullYear()} TravelAgency. Все права защищены.
          </p>
          <div className="flex gap-4">
            {['VK', 'TG', 'YT'].map((social) => (
              <a
                key={social}
                href="#"
                className={clsx(
                  'flex h-9 w-9 items-center justify-center rounded-full',
                  'bg-white/5 text-xs font-bold text-white/50 transition-colors hover:bg-terracotta hover:text-white',
                )}
              >
                {social}
              </a>
            ))}
          </div>
        </div>
      </div>
    </footer>
  );
}

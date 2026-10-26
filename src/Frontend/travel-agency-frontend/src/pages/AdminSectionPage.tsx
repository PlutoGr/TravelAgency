import { Link } from 'react-router-dom';
import { PageTransition } from '@/components/common';

const SUBSECTIONS = [
  { to: '/admin/users', label: 'Пользователи' },
  { to: '/admin/settings', label: 'Настройки сайта' },
] as const;

export default function AdminSectionPage() {
  return (
    <PageTransition>
      <h1 className="font-heading text-2xl font-bold text-dark sm:text-3xl">
        Администрирование
      </h1>
      <nav className="mt-8 flex max-w-xl flex-col gap-3" aria-label="Подразделы администрирования">
        {SUBSECTIONS.map((item) => (
          <Link
            key={item.to}
            to={item.to}
            className="rounded-2xl border border-sand bg-white px-5 py-4 font-medium text-dark transition-colors hover:border-primary hover:text-primary"
          >
            {item.label}
          </Link>
        ))}
      </nav>
    </PageTransition>
  );
}

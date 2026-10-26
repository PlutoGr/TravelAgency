import { LayoutDashboard, Briefcase, Map, Settings, Users } from 'lucide-react';
import { hasAtLeastRole } from '@/domain/roles';
import { useAuthStore } from '@/store/authStore';
import Header from './Header';
import Sidebar from './Sidebar';
import type { SidebarItem } from './Sidebar';
import RoutedOutlet from './RoutedOutlet';

const SIDEBAR_ITEMS: SidebarItem[] = [
  { icon: LayoutDashboard, label: 'Дашборд', path: '/manager' },
  { icon: Briefcase, label: 'Бронирования', path: '/manager/bookings' },
  { icon: Map, label: 'Туры', path: '/manager/tours' },
  { icon: Users, label: 'Клиенты', path: '/manager/clients' },
];

const ADMIN_SECTION_ITEM: SidebarItem = {
  icon: Settings,
  label: 'Администрирование',
  path: '/admin',
};

export default function ManagerLayout() {
  const role = useAuthStore((s) => s.user?.role);
  const items = hasAtLeastRole(role, 'admin')
    ? [...SIDEBAR_ITEMS, ADMIN_SECTION_ITEM]
    : SIDEBAR_ITEMS;

  return (
    <div className="flex min-h-screen flex-col">
      <Header />
      <div className="flex flex-1 pt-16 lg:pt-20">
        <Sidebar items={items} title="Панель менеджера" />
        <main className="flex-1 overflow-y-auto bg-cream p-4 lg:p-8">
          <RoutedOutlet />
        </main>
      </div>
    </div>
  );
}

import { LayoutDashboard, Settings, Users } from 'lucide-react';
import Header from './Header';
import Sidebar from './Sidebar';
import type { SidebarItem } from './Sidebar';
import RoutedOutlet from './RoutedOutlet';

const SIDEBAR_ITEMS: SidebarItem[] = [
  { icon: LayoutDashboard, label: 'Панель менеджера', path: '/manager' },
  { icon: Users, label: 'Пользователи', path: '/admin/users' },
  { icon: Settings, label: 'Настройки сайта', path: '/admin/settings' },
];

export default function AdminLayout() {
  return (
    <div className="flex min-h-screen flex-col">
      <Header />
      <div className="flex flex-1 pt-16 lg:pt-20">
        <Sidebar items={SIDEBAR_ITEMS} title="Администрирование" />
        <main className="flex-1 overflow-y-auto bg-cream p-4 lg:p-8">
          <RoutedOutlet />
        </main>
      </div>
    </div>
  );
}

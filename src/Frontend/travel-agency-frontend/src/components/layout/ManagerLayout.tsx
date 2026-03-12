import { Outlet } from 'react-router-dom';
import { LayoutDashboard, Briefcase, Map, Users } from 'lucide-react';
import Header from './Header';
import Sidebar from './Sidebar';
import type { SidebarItem } from './Sidebar';

const SIDEBAR_ITEMS: SidebarItem[] = [
  { icon: LayoutDashboard, label: 'Дашборд', path: '/manager' },
  { icon: Briefcase, label: 'Бронирования', path: '/manager/bookings' },
  { icon: Map, label: 'Туры', path: '/manager/tours' },
  { icon: Users, label: 'Клиенты', path: '/manager/clients' },
];

export default function ManagerLayout() {
  return (
    <div className="flex min-h-screen flex-col">
      <Header />
      <div className="flex flex-1 pt-16 lg:pt-20">
        <Sidebar items={SIDEBAR_ITEMS} title="Панель менеджера" />
        <main className="flex-1 overflow-y-auto bg-cream p-4 lg:p-8">
          <Outlet />
        </main>
      </div>
    </div>
  );
}

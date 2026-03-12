import { Outlet } from 'react-router-dom';
import { LayoutDashboard, Briefcase, Heart, User } from 'lucide-react';
import Header from './Header';
import Sidebar from './Sidebar';
import type { SidebarItem } from './Sidebar';

const SIDEBAR_ITEMS: SidebarItem[] = [
  { icon: LayoutDashboard, label: 'Главная', path: '/dashboard' },
  { icon: Briefcase, label: 'Мои бронирования', path: '/dashboard/bookings' },
  { icon: Heart, label: 'Избранное', path: '/dashboard/favorites' },
  { icon: User, label: 'Профиль', path: '/dashboard/profile' },
];

export default function DashboardLayout() {
  return (
    <div className="flex min-h-screen flex-col">
      <Header />
      <div className="flex flex-1 pt-16 lg:pt-20">
        <Sidebar items={SIDEBAR_ITEMS} title="Личный кабинет" />
        <main className="flex-1 overflow-y-auto bg-cream p-4 lg:p-8">
          <Outlet />
        </main>
      </div>
    </div>
  );
}

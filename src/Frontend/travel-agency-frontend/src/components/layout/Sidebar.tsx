import { useState } from 'react';
import { NavLink } from 'react-router-dom';
import { motion, AnimatePresence } from 'framer-motion';
import { Menu, X } from 'lucide-react';
import clsx from 'clsx';
import { useAuthStore } from '@/store/authStore';
import { Avatar } from '@/components/ui';
import type { LucideIcon } from 'lucide-react';

export interface SidebarItem {
  icon: LucideIcon;
  label: string;
  path: string;
}

interface SidebarProps {
  items: SidebarItem[];
  title: string;
}

export default function Sidebar({ items, title }: SidebarProps) {
  const [mobileOpen, setMobileOpen] = useState(false);
  const user = useAuthStore((s) => s.user);
  const fullName = user ? `${user.firstName} ${user.lastName}` : '';

  const navContent = (
    <>
      {/* User Info */}
      {user && (
        <div className="mb-6 flex items-center gap-3 px-3">
          <Avatar name={fullName} src={user.avatar} size="md" />
          <div className="min-w-0">
            <p className="truncate text-sm font-semibold text-dark">
              {fullName}
            </p>
            <p className="truncate text-xs text-warm-gray">{user.email}</p>
          </div>
        </div>
      )}

      {/* Nav Items */}
      <nav className="flex flex-col gap-1">
        {items.map(({ icon: Icon, label, path }) => (
          <NavLink
            key={path}
            to={path}
            end={path === items[0]?.path}
            onClick={() => setMobileOpen(false)}
            className={({ isActive }) =>
              clsx(
                'relative flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium transition-colors',
                isActive
                  ? 'bg-primary/5 text-primary'
                  : 'text-dark/60 hover:bg-sand hover:text-dark',
              )
            }
          >
            {({ isActive }) => (
              <>
                {isActive && (
                  <motion.div
                    layoutId={`sidebar-active-${title}`}
                    className="absolute left-0 top-1/2 h-6 w-1 -translate-y-1/2 rounded-r-full bg-primary"
                    transition={{ type: 'spring', stiffness: 400, damping: 30 }}
                  />
                )}
                <Icon size={18} />
                {label}
              </>
            )}
          </NavLink>
        ))}
      </nav>
    </>
  );

  return (
    <>
      {/* Desktop Sidebar */}
      <aside className="hidden w-64 shrink-0 border-r border-sand bg-white p-4 lg:block">
        <h2 className="mb-6 px-3 font-heading text-lg font-semibold text-dark">
          {title}
        </h2>
        {navContent}
      </aside>

      {/* Mobile Toggle */}
      <div className="fixed bottom-4 left-4 z-30 lg:hidden">
        <motion.button
          whileTap={{ scale: 0.9 }}
          onClick={() => setMobileOpen(!mobileOpen)}
          className="flex h-12 w-12 items-center justify-center rounded-full bg-primary text-white shadow-button"
        >
          {mobileOpen ? <X size={20} /> : <Menu size={20} />}
        </motion.button>
      </div>

      {/* Mobile Sidebar */}
      <AnimatePresence>
        {mobileOpen && (
          <>
            <motion.div
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              exit={{ opacity: 0 }}
              className="fixed inset-0 z-20 bg-dark/30 backdrop-blur-sm lg:hidden"
              onClick={() => setMobileOpen(false)}
            />
            <motion.aside
              initial={{ x: -280 }}
              animate={{ x: 0 }}
              exit={{ x: -280 }}
              transition={{ type: 'spring', stiffness: 350, damping: 30 }}
              className="fixed left-0 top-0 z-20 h-full w-64 bg-white p-4 shadow-card lg:hidden"
            >
              <h2 className="mb-6 px-3 font-heading text-lg font-semibold text-dark">
                {title}
              </h2>
              {navContent}
            </motion.aside>
          </>
        )}
      </AnimatePresence>
    </>
  );
}

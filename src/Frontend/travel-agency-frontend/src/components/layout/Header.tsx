import { useState, useEffect, useRef } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { motion, AnimatePresence } from 'framer-motion';
import {
  Heart,
  Menu,
  X,
  User,
  LogOut,
  LayoutDashboard,
  ChevronDown,
} from 'lucide-react';
import clsx from 'clsx';
import { useAuthStore } from '@/store/authStore';
import { useUIStore } from '@/store/uiStore';
import { useFavoritesStore } from '@/store/favoritesStore';
import { Button, Avatar } from '@/components/ui';

const NAV_LINKS = [
  { label: 'Туры', href: '/tours' },
  { label: 'Направления', href: '/tours#destinations' },
  { label: 'Отзывы', href: '/#reviews' },
  { label: 'Контакты', href: '/#contacts' },
] as const;

const SCROLL_THRESHOLD = 50;

export default function Header() {
  const [scrolled, setScrolled] = useState(false);
  const [dropdownOpen, setDropdownOpen] = useState(false);
  const dropdownRef = useRef<HTMLDivElement>(null);
  const navigate = useNavigate();

  const { user, isAuthenticated, logout } = useAuthStore();
  const { isMobileMenuOpen, toggleMobileMenu, closeMobileMenu, openAuthModal } =
    useUIStore();
  const favoriteCount = useFavoritesStore((s) => s.count);

  useEffect(() => {
    const handleScroll = () => setScrolled(window.scrollY > SCROLL_THRESHOLD);
    window.addEventListener('scroll', handleScroll, { passive: true });
    return () => window.removeEventListener('scroll', handleScroll);
  }, []);

  useEffect(() => {
    function handleClickOutside(e: MouseEvent) {
      if (dropdownRef.current && !dropdownRef.current.contains(e.target as Node)) {
        setDropdownOpen(false);
      }
    }
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  function handleNavClick(href: string) {
    closeMobileMenu();
    if (href.includes('#')) {
      const [path, hash] = href.split('#');
      if (window.location.pathname === (path || '/')) {
        document.getElementById(hash)?.scrollIntoView({ behavior: 'smooth' });
      } else {
        navigate(path || '/');
        setTimeout(() => {
          document.getElementById(hash)?.scrollIntoView({ behavior: 'smooth' });
        }, 100);
      }
    } else {
      navigate(href);
    }
  }

  function handleLogout() {
    logout();
    setDropdownOpen(false);
    navigate('/');
  }

  const fullName = user ? `${user.firstName} ${user.lastName}` : '';

  return (
    <header
      className={clsx(
        'fixed top-0 left-0 right-0 z-40 transition-all duration-300',
        scrolled
          ? 'bg-white/80 backdrop-blur-lg shadow-header'
          : 'bg-transparent',
      )}
    >
      <div className="mx-auto flex h-16 max-w-7xl items-center justify-between px-4 lg:h-20 lg:px-8">
        {/* Logo */}
        <Link
          to="/"
          className="font-heading text-xl font-bold text-primary lg:text-2xl"
          onClick={closeMobileMenu}
        >
          TravelAgency
        </Link>

        {/* Desktop Nav */}
        <nav className="hidden items-center gap-8 md:flex">
          {NAV_LINKS.map((link) => (
            <button
              key={link.href}
              onClick={() => handleNavClick(link.href)}
              className="font-medium text-dark/70 transition-colors hover:text-primary"
            >
              {link.label}
            </button>
          ))}
        </nav>

        {/* Right Side */}
        <div className="hidden items-center gap-3 md:flex">
          <Link
            to="/dashboard/favorites"
            className="relative rounded-full p-2 text-dark/70 transition-colors hover:bg-sand hover:text-primary"
          >
            <Heart size={20} />
            {favoriteCount > 0 && (
              <motion.span
                initial={{ scale: 0 }}
                animate={{ scale: 1 }}
                className="absolute -right-0.5 -top-0.5 flex h-4.5 w-4.5 items-center justify-center rounded-full bg-terracotta text-[10px] font-bold text-white"
              >
                {favoriteCount}
              </motion.span>
            )}
          </Link>

          {isAuthenticated && user ? (
            <div className="relative" ref={dropdownRef}>
              <button
                onClick={() => setDropdownOpen(!dropdownOpen)}
                className="flex items-center gap-2 rounded-full py-1 pl-1 pr-3 transition-colors hover:bg-sand"
              >
                <Avatar name={fullName} src={user.avatar} size="sm" />
                <span className="max-w-[120px] truncate text-sm font-medium text-dark">
                  {user.firstName}
                </span>
                <ChevronDown
                  size={14}
                  className={clsx(
                    'text-warm-gray transition-transform',
                    dropdownOpen && 'rotate-180',
                  )}
                />
              </button>

              <AnimatePresence>
                {dropdownOpen && (
                  <motion.div
                    initial={{ opacity: 0, y: 8 }}
                    animate={{ opacity: 1, y: 0 }}
                    exit={{ opacity: 0, y: 8 }}
                    transition={{ duration: 0.15 }}
                    className="absolute right-0 top-full mt-2 w-56 overflow-hidden rounded-2xl border border-sand bg-white py-2 shadow-card"
                  >
                    <div className="border-b border-sand px-4 pb-2 pt-1">
                      <p className="truncate text-sm font-semibold text-dark">
                        {fullName}
                      </p>
                      <p className="truncate text-xs text-warm-gray">
                        {user.email}
                      </p>
                    </div>

                    <div className="py-1">
                      <DropdownLink
                        icon={LayoutDashboard}
                        label="Личный кабинет"
                        onClick={() => {
                          navigate('/dashboard');
                          setDropdownOpen(false);
                        }}
                      />
                      <DropdownLink
                        icon={Heart}
                        label="Мои бронирования"
                        onClick={() => {
                          navigate('/dashboard/bookings');
                          setDropdownOpen(false);
                        }}
                      />
                      <DropdownLink
                        icon={User}
                        label="Профиль"
                        onClick={() => {
                          navigate('/dashboard/profile');
                          setDropdownOpen(false);
                        }}
                      />
                      {user.role === 'manager' && (
                        <DropdownLink
                          icon={LayoutDashboard}
                          label="Панель менеджера"
                          onClick={() => {
                            navigate('/manager');
                            setDropdownOpen(false);
                          }}
                        />
                      )}
                    </div>

                    <div className="border-t border-sand pt-1">
                      <DropdownLink
                        icon={LogOut}
                        label="Выход"
                        onClick={handleLogout}
                        danger
                      />
                    </div>
                  </motion.div>
                )}
              </AnimatePresence>
            </div>
          ) : (
            <Button
              size="sm"
              onClick={() => openAuthModal('login')}
            >
              Войти
            </Button>
          )}
        </div>

        {/* Mobile Controls */}
        <div className="flex items-center gap-2 md:hidden">
          <Link
            to="/dashboard/favorites"
            className="relative rounded-full p-2 text-dark/70"
          >
            <Heart size={20} />
            {favoriteCount > 0 && (
              <span className="absolute -right-0.5 -top-0.5 flex h-4 w-4 items-center justify-center rounded-full bg-terracotta text-[10px] font-bold text-white">
                {favoriteCount}
              </span>
            )}
          </Link>
          <button
            onClick={toggleMobileMenu}
            className="rounded-full p-2 text-dark/70 transition-colors hover:bg-sand"
          >
            {isMobileMenuOpen ? <X size={22} /> : <Menu size={22} />}
          </button>
        </div>
      </div>

      {/* Mobile Menu */}
      <AnimatePresence>
        {isMobileMenuOpen && (
          <motion.div
            initial={{ opacity: 0, height: 0 }}
            animate={{ opacity: 1, height: '100dvh' }}
            exit={{ opacity: 0, height: 0 }}
            transition={{ duration: 0.3, ease: 'easeInOut' }}
            className="fixed inset-0 top-16 z-30 overflow-y-auto bg-white md:hidden"
          >
            <nav className="flex flex-col gap-2 p-6">
              {NAV_LINKS.map((link) => (
                <button
                  key={link.href}
                  onClick={() => handleNavClick(link.href)}
                  className="rounded-xl px-4 py-3 text-left text-lg font-medium text-dark transition-colors hover:bg-sand"
                >
                  {link.label}
                </button>
              ))}

              <div className="my-4 h-px bg-sand" />

              {isAuthenticated && user ? (
                <>
                  <div className="mb-2 flex items-center gap-3 px-4">
                    <Avatar name={fullName} src={user.avatar} size="md" />
                    <div>
                      <p className="font-semibold text-dark">{fullName}</p>
                      <p className="text-sm text-warm-gray">{user.email}</p>
                    </div>
                  </div>
                  <button
                    onClick={() => {
                      navigate('/dashboard');
                      closeMobileMenu();
                    }}
                    className="rounded-xl px-4 py-3 text-left text-lg font-medium text-dark transition-colors hover:bg-sand"
                  >
                    Личный кабинет
                  </button>
                  <button
                    onClick={() => {
                      navigate('/dashboard/bookings');
                      closeMobileMenu();
                    }}
                    className="rounded-xl px-4 py-3 text-left text-lg font-medium text-dark transition-colors hover:bg-sand"
                  >
                    Мои бронирования
                  </button>
                  {user.role === 'manager' && (
                    <button
                      onClick={() => {
                        navigate('/manager');
                        closeMobileMenu();
                      }}
                      className="rounded-xl px-4 py-3 text-left text-lg font-medium text-primary transition-colors hover:bg-sand"
                    >
                      Панель менеджера
                    </button>
                  )}
                  <button
                    onClick={() => {
                      handleLogout();
                      closeMobileMenu();
                    }}
                    className="rounded-xl px-4 py-3 text-left text-lg font-medium text-red-500 transition-colors hover:bg-red-50"
                  >
                    Выход
                  </button>
                </>
              ) : (
                <Button
                  fullWidth
                  onClick={() => {
                    closeMobileMenu();
                    openAuthModal('login');
                  }}
                >
                  Войти
                </Button>
              )}
            </nav>
          </motion.div>
        )}
      </AnimatePresence>
    </header>
  );
}

function DropdownLink({
  icon: Icon,
  label,
  onClick,
  danger = false,
}: {
  icon: typeof User;
  label: string;
  onClick: () => void;
  danger?: boolean;
}) {
  return (
    <button
      onClick={onClick}
      className={clsx(
        'flex w-full items-center gap-3 px-4 py-2 text-sm transition-colors',
        danger
          ? 'text-red-500 hover:bg-red-50'
          : 'text-dark/80 hover:bg-sand hover:text-primary',
      )}
    >
      <Icon size={16} />
      {label}
    </button>
  );
}

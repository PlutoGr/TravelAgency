import { lazy, Suspense, useEffect, type ReactElement } from 'react';
import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { Toaster } from 'react-hot-toast';
import { setAuthSessionHandler } from '@/api/client';
import { afterSessionEstablished } from '@/store/authIntent';
import { useAuthStore } from '@/store/authStore';
import {
  PublicLayout,
  DashboardLayout,
  ManagerLayout,
  AdminLayout,
  ProtectedRoute,
  AuthModal,
  AuthRedirectHandler,
  ScrollToTop,
} from '@/components/layout';
import {
  ADMIN_AREA_ROLES,
  ADMIN_ROUTE_DEFS,
  MANAGER_AREA_ROLES,
  MANAGER_ROUTE_DEFS,
} from '@/routes/staffRoutes';
import { Skeleton } from '@/components/ui';
import AdminPlaceholderPage from '@/pages/AdminPlaceholderPage';
import AdminSectionPage from '@/pages/AdminSectionPage';
import AdminSiteSettingsPage from '@/pages/AdminSiteSettingsPage';

const HomePage = lazy(() => import('@/pages/HomePage'));
const TourCatalogPage = lazy(() => import('@/pages/TourCatalogPage'));
const TourDetailPage = lazy(() => import('@/pages/TourDetailPage'));

const DashboardPage = lazy(() => import('@/pages/DashboardPage'));
const MyBookingsPage = lazy(() => import('@/pages/MyBookingsPage'));
const BookingDetailPage = lazy(() => import('@/pages/BookingDetailPage'));
const ProfilePage = lazy(() => import('@/pages/ProfilePage'));
const FavoritesPage = lazy(() => import('@/pages/FavoritesPage'));

const ManagerDashboardPage = lazy(() => import('@/pages/ManagerDashboardPage'));
const ManagerBookingsPage = lazy(() => import('@/pages/ManagerBookingsPage'));
const ManagerBookingDetailPage = lazy(() => import('@/pages/ManagerBookingDetailPage'));
const ManagerToursPage = lazy(() => import('@/pages/ManagerToursPage'));
const TourWizardPage = lazy(() => import('@/pages/TourWizardPage'));
const ManagerClientsPage = lazy(() => import('@/pages/ManagerClientsPage'));

const NotFoundPage = lazy(() => import('@/pages/NotFoundPage'));

function managerRouteElement(path: string): ReactElement | null {
  switch (path) {
    case 'manager':
      return <ManagerDashboardPage />;
    case 'manager/bookings':
      return <ManagerBookingsPage />;
    case 'manager/bookings/:id':
      return <ManagerBookingDetailPage />;
    case 'manager/tours/new':
    case 'manager/tours/:tourId':
      return <TourWizardPage />;
    case 'manager/tours':
      return <ManagerToursPage />;
    case 'manager/clients':
      return <ManagerClientsPage />;
    default:
      return null;
  }
}

function adminRouteElement(path: string): ReactElement | null {
  switch (path) {
    case 'admin':
      return <AdminSectionPage />;
    case 'admin/users':
      return <AdminPlaceholderPage title="Пользователи" />;
    case 'admin/settings':
      return <AdminSiteSettingsPage />;
    default:
      return null;
  }
}

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 5 * 60 * 1000,
      retry: 1,
    },
  },
});

function PageFallback() {
  return (
    <div className="flex min-h-[40vh] items-center justify-center p-8">
      <div className="w-full max-w-md space-y-4">
        <Skeleton className="h-8 w-3/4" />
        <Skeleton className="h-4 w-full" />
        <Skeleton className="h-4 w-5/6" />
      </div>
    </div>
  );
}

export function AppRoutes() {
  return (
    <Suspense fallback={<PageFallback />}>
      <Routes>
        {/* Public Routes */}
        <Route element={<PublicLayout />}>
          <Route index element={<HomePage />} />
          <Route path="tours" element={<TourCatalogPage />} />
          <Route path="tours/:id" element={<TourDetailPage />} />
        </Route>

        {/* Client Dashboard */}
        <Route element={<ProtectedRoute allowedRoles={['client', 'manager', 'admin']} />}>
          <Route element={<DashboardLayout />}>
            <Route path="dashboard" element={<DashboardPage />} />
            <Route path="dashboard/bookings" element={<MyBookingsPage />} />
            <Route path="dashboard/bookings/:id" element={<BookingDetailPage />} />
            <Route path="dashboard/profile" element={<ProfilePage />} />
            <Route path="dashboard/favorites" element={<FavoritesPage />} />
          </Route>
        </Route>

        {/* Панель менеджера: админ проходит по иерархии ролей. */}
        <Route element={<ProtectedRoute allowedRoles={MANAGER_AREA_ROLES} />}>
          <Route element={<ManagerLayout />}>
            {MANAGER_ROUTE_DEFS.map((route) => (
              <Route key={route.path} path={route.path} element={managerRouteElement(route.path)} />
            ))}
          </Route>
        </Route>

        {/* Администрирование: только админ. */}
        <Route element={<ProtectedRoute allowedRoles={ADMIN_AREA_ROLES} />}>
          <Route element={<AdminLayout />}>
            {ADMIN_ROUTE_DEFS.map((route) => (
              <Route key={route.path} path={route.path} element={adminRouteElement(route.path)} />
            ))}
          </Route>
        </Route>

        {/* 404 */}
        <Route path="*" element={<PublicLayout />}>
          <Route path="*" element={<NotFoundPage />} />
        </Route>
      </Routes>
    </Suspense>
  );
}

export default function App() {
  const checkAuth = useAuthStore((s) => s.checkAuth);
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);

  useEffect(() => {
    setAuthSessionHandler(() => {
      useAuthStore.getState().markSessionExpired();
    });
    return () => setAuthSessionHandler(null);
  }, []);

  useEffect(() => {
    checkAuth();
  }, [checkAuth]);

  useEffect(() => {
    if (!isAuthenticated) return;
    let cancelled = false;
    void afterSessionEstablished(() => cancelled);
    return () => {
      cancelled = true;
    };
  }, [isAuthenticated]);

  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <ScrollToTop />
        <AuthRedirectHandler />
        <AppRoutes />
        <AuthModal />
      </BrowserRouter>
      <Toaster
        position="top-right"
        toastOptions={{
          duration: 4000,
          style: {
            borderRadius: '12px',
            background: '#1A1A2E',
            color: '#fff',
            fontSize: '14px',
            padding: '12px 16px',
          },
          success: {
            iconTheme: {
              primary: '#8B9E6B',
              secondary: '#fff',
            },
          },
          error: {
            iconTheme: {
              primary: '#ef4444',
              secondary: '#fff',
            },
          },
        }}
      />
    </QueryClientProvider>
  );
}

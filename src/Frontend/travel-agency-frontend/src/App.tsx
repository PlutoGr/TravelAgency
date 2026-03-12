import { lazy, Suspense, useEffect } from 'react';
import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { Toaster } from 'react-hot-toast';
import { useAuthStore } from '@/store/authStore';
import { useFavoritesStore } from '@/store/favoritesStore';
import {
  PublicLayout,
  DashboardLayout,
  ManagerLayout,
  ProtectedRoute,
  AuthModal,
  ScrollToTop,
} from '@/components/layout';
import { Skeleton } from '@/components/ui';

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
const ManagerClientsPage = lazy(() => import('@/pages/ManagerClientsPage'));

const NotFoundPage = lazy(() => import('@/pages/NotFoundPage'));

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

export default function App() {
  const checkAuth = useAuthStore((s) => s.checkAuth);
  const loadFavorites = useFavoritesStore((s) => s.loadFavorites);

  useEffect(() => {
    checkAuth();
    loadFavorites();
  }, [checkAuth, loadFavorites]);

  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <ScrollToTop />
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

            {/* Manager Panel */}
            <Route element={<ProtectedRoute allowedRoles={['manager', 'admin']} />}>
              <Route element={<ManagerLayout />}>
                <Route path="manager" element={<ManagerDashboardPage />} />
                <Route path="manager/bookings" element={<ManagerBookingsPage />} />
                <Route path="manager/bookings/:id" element={<ManagerBookingDetailPage />} />
                <Route path="manager/tours" element={<ManagerToursPage />} />
                <Route path="manager/clients" element={<ManagerClientsPage />} />
              </Route>
            </Route>

            {/* 404 */}
            <Route path="*" element={<PublicLayout />}>
              <Route path="*" element={<NotFoundPage />} />
            </Route>
          </Routes>
        </Suspense>
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

import { useEffect } from 'react';
import { Outlet, Navigate, useLocation } from 'react-router-dom';
import { useAuthStore } from '@/store/authStore';
import { useUIStore } from '@/store/uiStore';
import { Skeleton } from '@/components/ui';
import { resolveReturnTo } from '@/utils/safeReturnTo';

interface ProtectedRouteProps {
  allowedRoles?: ('client' | 'manager' | 'admin')[];
}

function AuthLoadingFallback() {
  return (
    <div
      role="status"
      aria-live="polite"
      aria-label="Проверка входа"
      className="flex min-h-[40vh] items-center justify-center p-8"
    >
      <div className="w-full max-w-md space-y-4">
        <Skeleton className="h-8 w-3/4" />
        <Skeleton className="h-4 w-full" />
        <Skeleton className="h-4 w-5/6" />
      </div>
    </div>
  );
}

export default function ProtectedRoute({ allowedRoles }: ProtectedRouteProps) {
  const location = useLocation();
  const { isAuthenticated, isLoading, user } = useAuthStore();
  const openAuthModal = useUIStore((s) => s.openAuthModal);

  useEffect(() => {
    if (!isLoading && !isAuthenticated) {
      openAuthModal('login');
    }
  }, [isLoading, isAuthenticated, openAuthModal]);

  if (isLoading) {
    return <AuthLoadingFallback />;
  }

  if (!isAuthenticated) {
    const requested = `${location.pathname}${location.search}`;
    const returnTo = encodeURIComponent(resolveReturnTo(requested));
    return <Navigate to={`/?auth=login&returnTo=${returnTo}`} replace />;
  }

  if (allowedRoles && user && !allowedRoles.includes(user.role)) {
    return <Navigate to="/" replace />;
  }

  return <Outlet />;
}

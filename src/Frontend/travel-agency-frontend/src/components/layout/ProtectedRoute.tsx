import { useEffect } from 'react';
import { Outlet, Navigate } from 'react-router-dom';
import { useAuthStore } from '@/store/authStore';
import { useUIStore } from '@/store/uiStore';

interface ProtectedRouteProps {
  allowedRoles?: ('client' | 'manager' | 'admin')[];
}

export default function ProtectedRoute({ allowedRoles }: ProtectedRouteProps) {
  const { isAuthenticated, user } = useAuthStore();
  const openAuthModal = useUIStore((s) => s.openAuthModal);

  useEffect(() => {
    if (!isAuthenticated) {
      openAuthModal('login');
    }
  }, [isAuthenticated, openAuthModal]);

  if (!isAuthenticated) {
    return <Navigate to="/" replace />;
  }

  if (allowedRoles && user && !allowedRoles.includes(user.role)) {
    return <Navigate to="/" replace />;
  }

  return <Outlet />;
}

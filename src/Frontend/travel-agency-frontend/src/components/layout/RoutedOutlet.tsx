import { Outlet, useLocation } from 'react-router-dom';
import PageErrorBoundary from '@/components/common/PageErrorBoundary';

/** Контент страницы внутри макета. Ошибка одного экрана не сносит оболочку. */
export default function RoutedOutlet() {
  const { pathname } = useLocation();

  return (
    <PageErrorBoundary key={pathname}>
      <Outlet />
    </PageErrorBoundary>
  );
}

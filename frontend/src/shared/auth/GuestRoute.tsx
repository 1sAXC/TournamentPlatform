import { Navigate, Outlet } from 'react-router-dom';
import { useAuth } from './useAuth';

export function GuestRoute() {
  const { isAuthenticated, isHydrated } = useAuth();
  if (!isHydrated) return null;
  if (isAuthenticated) return <Navigate to="/" replace />;
  return <Outlet />;
}

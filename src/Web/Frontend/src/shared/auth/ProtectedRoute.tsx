import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { useAuth } from './useAuth';
import type { Role } from '@/shared/api/types';

interface Props {
  roles?: Role[];
  requireActiveOrganizer?: boolean;
}

export function ProtectedRoute({ roles, requireActiveOrganizer }: Props) {
  const { isAuthenticated, role, user, isHydrated } = useAuth();
  const location = useLocation();

  if (!isHydrated) return null;

  if (!isAuthenticated) {
    return <Navigate to="/login" state={{ from: location.pathname }} replace />;
  }
  if (roles && role && !roles.includes(role)) {
    return <Navigate to="/" replace />;
  }
  if (requireActiveOrganizer && (role !== 'Organizer' || user?.accountStatus !== 'Active')) {
    return <Navigate to="/organizer/pending" replace />;
  }
  return <Outlet />;
}

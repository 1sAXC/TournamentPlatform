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
  // Admin bypasses the active-organizer requirement: routes that are
  // organizer's home turf (creating/managing tournaments) are also legitimate
  // admin oversight surfaces and the backend allows them via the
  // RequireOrganizerOrAdmin policy. Pending/rejected organizers still bounce.
  if (
    requireActiveOrganizer
    && role !== 'Admin'
    && (role !== 'Organizer' || user?.accountStatus !== 'Active')
  ) {
    return <Navigate to="/organizer/pending" replace />;
  }
  return <Outlet />;
}

import { Navigate } from 'react-router-dom';
import { useAuth } from '@/shared/auth/useAuth';

export function RoleHome() {
  const { isAuthenticated, role, isHydrated } = useAuth();
  if (!isHydrated) return null;
  if (!isAuthenticated) return <Navigate to="/login" replace />;
  if (role === 'Admin') return <Navigate to="/admin/applications" replace />;
  if (role === 'Organizer') return <Navigate to="/organizer" replace />;
  return <Navigate to="/home" replace />;
}

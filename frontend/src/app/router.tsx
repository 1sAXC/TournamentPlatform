import { createBrowserRouter, RouterProvider } from 'react-router-dom';
import { ProtectedRoute } from '@/shared/auth/ProtectedRoute';
import { GuestRoute } from '@/shared/auth/GuestRoute';
import { RoleHome } from './RoleHome';

import { LoginPage } from '@/pages/guest/LoginPage';
import { RegisterPlayerPage } from '@/pages/guest/RegisterPlayerPage';
import { RegisterOrganizerPage } from '@/pages/guest/RegisterOrganizerPage';

import { PlayerHomePage } from '@/pages/player/PlayerHomePage';
import { TournamentsCatalogPage } from '@/pages/player/TournamentsCatalogPage';
import { TournamentDetailPage } from '@/pages/player/TournamentDetailPage';
import { MyTournamentsPage } from '@/pages/player/MyTournamentsPage';
import { PlayerProfilePage } from '@/pages/player/PlayerProfilePage';

import { OrgTournamentsPage } from '@/pages/organizer/OrgTournamentsPage';
import { OrgCreatePage } from '@/pages/organizer/OrgCreatePage';
import { OrgManagePage } from '@/pages/organizer/OrgManagePage';
import { OrgProfilePage } from '@/pages/organizer/OrgProfilePage';
import { OrgPendingPage } from '@/pages/organizer/OrgPendingPage';

import { AdminUsersPage } from '@/pages/admin/AdminUsersPage';
import { AdminApplicationsPage } from '@/pages/admin/AdminApplicationsPage';
import { AdminTournamentsPage } from '@/pages/admin/AdminTournamentsPage';

import { NotFoundPage } from '@/pages/NotFoundPage';

const router = createBrowserRouter([
  { path: '/', element: <RoleHome /> },
  {
    element: <GuestRoute />,
    children: [
      { path: '/login', element: <LoginPage /> },
      { path: '/register/player', element: <RegisterPlayerPage /> },
      { path: '/register/organizer', element: <RegisterOrganizerPage /> },
    ],
  },
  {
    element: <ProtectedRoute roles={['Player']} />,
    children: [
      { path: '/home', element: <PlayerHomePage /> },
      { path: '/my-tournaments', element: <MyTournamentsPage /> },
      { path: '/profile', element: <PlayerProfilePage /> },
    ],
  },
  {
    // Tournament catalog & details are accessible to any signed-in user
    // (player browses; organizer browses other tournaments; admin oversees).
    element: <ProtectedRoute roles={['Player', 'Organizer', 'Admin']} />,
    children: [
      { path: '/tournaments', element: <TournamentsCatalogPage /> },
      { path: '/tournaments/:id', element: <TournamentDetailPage /> },
    ],
  },
  {
    element: <ProtectedRoute roles={['Organizer']} />,
    children: [
      { path: '/organizer/pending', element: <OrgPendingPage /> },
      { path: '/organizer/profile', element: <OrgProfilePage /> },
    ],
  },
  {
    element: <ProtectedRoute roles={['Organizer']} requireActiveOrganizer />,
    children: [
      { path: '/organizer', element: <OrgTournamentsPage /> },
      { path: '/organizer/create', element: <OrgCreatePage /> },
      { path: '/organizer/tournaments/:id', element: <OrgManagePage /> },
    ],
  },
  {
    element: <ProtectedRoute roles={['Admin']} />,
    children: [
      { path: '/admin/applications', element: <AdminApplicationsPage /> },
      { path: '/admin/users', element: <AdminUsersPage /> },
      { path: '/admin/tournaments', element: <AdminTournamentsPage /> },
    ],
  },
  { path: '*', element: <NotFoundPage /> },
]);

export function AppRouter() {
  return <RouterProvider router={router} />;
}

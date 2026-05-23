import type { NavLinkItem } from '@/shared/ui/TopBar';

export const playerNav: NavLinkItem[] = [
  { to: '/home', label: 'Главная', icon: 'grid', end: true },
  { to: '/tournaments', label: 'Турниры', icon: 'list' },
  { to: '/my-tournaments', label: 'Мои турниры', icon: 'trophy' },
];

export const organizerNav: NavLinkItem[] = [
  { to: '/organizer', label: 'Мои турниры', icon: 'trophy', end: true },
  { to: '/organizer/create', label: 'Создать турнир', icon: 'plus' },
  { to: '/tournaments', label: 'Все турниры', icon: 'list' },
];

export const adminNav: NavLinkItem[] = [
  { to: '/admin/users', label: 'Пользователи', icon: 'users' },
  { to: '/admin/applications', label: 'Заявки', icon: 'inbox' },
  { to: '/admin/tournaments', label: 'Турниры', icon: 'trophy' },
];

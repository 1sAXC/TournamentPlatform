import type { Role } from '@/shared/api/types';

const map: Record<string, string> = {
  Player: 'Игрок',
  Organizer: 'Организатор',
  Admin: 'Администратор',
};

const cssMap: Record<string, string> = {
  Player: 'player',
  Organizer: 'organizer',
  Admin: 'admin',
};

export function RoleBadge({ role }: { role: Role | string }) {
  return <span className={`role-badge ${cssMap[role] ?? 'player'}`}>{map[role] ?? role}</span>;
}

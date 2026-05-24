import { initials } from '@/shared/lib/initials';

interface Props {
  name?: string | null;
  size?: 'sm' | 'md' | 'lg';
  variant?: 'plr' | 'org' | 'adm';
}

export function Avatar({ name, size = 'md', variant }: Props) {
  const sizeCls = size === 'sm' ? 'av-sm' : size === 'lg' ? 'av-lg' : '';
  const variantCls = variant ? ` av-${variant}` : '';
  return <span className={`av ${sizeCls}${variantCls}`}>{initials(name)}</span>;
}

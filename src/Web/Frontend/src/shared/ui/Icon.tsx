import type { CSSProperties } from 'react';

export type IconName =
  | 'trophy' | 'list' | 'grid' | 'user' | 'users' | 'shield' | 'cog'
  | 'plus' | 'pen' | 'x' | 'check' | 'chev' | 'chevDown' | 'inbox'
  | 'flag' | 'cal' | 'bell' | 'lock' | 'out' | 'filter' | 'dot'
  | 'eye' | 'swap' | 'sun' | 'arrowRight';

interface Props {
  name: IconName;
  size?: number;
  style?: CSSProperties;
}

const stroke = {
  fill: 'none',
  stroke: 'currentColor',
  strokeWidth: 1.5,
  strokeLinecap: 'round' as const,
  strokeLinejoin: 'round' as const,
};

export function Icon({ name, size = 14, style }: Props) {
  const s: CSSProperties = { width: size, height: size, flexShrink: 0, ...style };
  switch (name) {
    case 'trophy':
      return <svg viewBox="0 0 24 24" style={s}><path {...stroke} d="M7 4h10v3a5 5 0 0 1-10 0V4z M4 6h3 M17 6h3 M10 17h4 M12 13v4 M9 21h6" /></svg>;
    case 'list':
      return <svg viewBox="0 0 24 24" style={s}><path {...stroke} d="M4 7h16 M4 12h16 M4 17h16" /></svg>;
    case 'grid':
      return <svg viewBox="0 0 24 24" style={s}><path {...stroke} d="M4 4h6v6H4z M14 4h6v6h-6z M4 14h6v6H4z M14 14h6v6h-6z" /></svg>;
    case 'user':
      return <svg viewBox="0 0 24 24" style={s}><path {...stroke} d="M12 12a4 4 0 1 0 0-8 4 4 0 0 0 0 8z M4 21c0-4 4-6 8-6s8 2 8 6" /></svg>;
    case 'users':
      return <svg viewBox="0 0 24 24" style={s}><path {...stroke} d="M9 11a3.5 3.5 0 1 0 0-7 3.5 3.5 0 0 0 0 7z M17 11a3 3 0 1 0 0-6 M3 20c0-3 3-5 6-5s6 2 6 5 M21 19c0-2-2-4-4-4" /></svg>;
    case 'shield':
      return <svg viewBox="0 0 24 24" style={s}><path {...stroke} d="M12 3l8 3v6c0 5-3.5 8-8 9-4.5-1-8-4-8-9V6l8-3z" /></svg>;
    case 'cog':
      return <svg viewBox="0 0 24 24" style={s}><circle {...stroke} cx="12" cy="12" r="3" /><path {...stroke} d="M12 2v3 M12 19v3 M2 12h3 M19 12h3 M5 5l2 2 M17 17l2 2 M5 19l2-2 M17 7l2-2" /></svg>;
    case 'plus':
      return <svg viewBox="0 0 24 24" style={s}><path {...stroke} d="M12 5v14 M5 12h14" /></svg>;
    case 'pen':
      return <svg viewBox="0 0 24 24" style={s}><path {...stroke} d="M4 20l4-1L20 7l-3-3L5 16l-1 4z" /></svg>;
    case 'x':
      return <svg viewBox="0 0 24 24" style={s}><path {...stroke} d="M6 6l12 12 M18 6L6 18" /></svg>;
    case 'check':
      return <svg viewBox="0 0 24 24" style={s}><path {...stroke} d="M5 12l5 5L20 7" /></svg>;
    case 'chev':
      return <svg viewBox="0 0 24 24" style={s}><path {...stroke} d="M9 6l6 6-6 6" /></svg>;
    case 'chevDown':
      return <svg viewBox="0 0 24 24" style={s}><path {...stroke} d="M6 9l6 6 6-6" /></svg>;
    case 'inbox':
      return <svg viewBox="0 0 24 24" style={s}><path {...stroke} d="M4 14l3-8h10l3 8v6H4z M4 14h5l1 2h4l1-2h5" /></svg>;
    case 'flag':
      return <svg viewBox="0 0 24 24" style={s}><path {...stroke} d="M5 21V4l8 3 6-2v10l-6 2-8-3v7" /></svg>;
    case 'cal':
      return <svg viewBox="0 0 24 24" style={s}><path {...stroke} d="M4 6h16v14H4z M4 10h16 M8 4v4 M16 4v4" /></svg>;
    case 'bell':
      return <svg viewBox="0 0 24 24" style={s}><path {...stroke} d="M6 16V11a6 6 0 1 1 12 0v5l1 2H5l1-2z M10 20a2 2 0 0 0 4 0" /></svg>;
    case 'lock':
      return <svg viewBox="0 0 24 24" style={s}><path {...stroke} d="M6 11h12v9H6z M8 11V8a4 4 0 0 1 8 0v3" /></svg>;
    case 'out':
      return <svg viewBox="0 0 24 24" style={s}><path {...stroke} d="M10 4H5v16h5 M15 9l3 3-3 3 M9 12h9" /></svg>;
    case 'filter':
      return <svg viewBox="0 0 24 24" style={s}><path {...stroke} d="M4 5h16l-6 8v6l-4-2v-4z" /></svg>;
    case 'dot':
      return <svg viewBox="0 0 24 24" style={s}><circle cx="12" cy="12" r="3" fill="currentColor" /></svg>;
    case 'eye':
      return <svg viewBox="0 0 24 24" style={s}><path {...stroke} d="M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7S2 12 2 12z" /><circle {...stroke} cx="12" cy="12" r="3" /></svg>;
    case 'swap':
      return <svg viewBox="0 0 24 24" style={s}><path {...stroke} d="M4 8h13l-3-3 M20 16H7l3 3" /></svg>;
    case 'sun':
      return <svg viewBox="0 0 24 24" style={s}><circle {...stroke} cx="12" cy="12" r="4" /><path {...stroke} d="M12 2v2 M12 20v2 M4 12H2 M22 12h-2 M5 5l1.5 1.5 M17.5 17.5L19 19 M5 19l1.5-1.5 M17.5 6.5L19 5" /></svg>;
    case 'arrowRight':
      return <svg viewBox="0 0 24 24" style={s}><path {...stroke} d="M5 12h14 M13 6l6 6-6 6" /></svg>;
    default:
      return null;
  }
}

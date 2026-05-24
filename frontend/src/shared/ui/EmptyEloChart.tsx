interface Props {
  message?: string;
}

/**
 * Static empty-state version of EloChart — same axes/gridlines layout
 * so the placeholder visually matches the populated chart, but with a
 * centered hint instead of a polyline.
 */
export function EmptyEloChart({ message = 'Нет данных' }: Props) {
  const W = 600, H = 200;
  const padL = 46, padR = 18, padT = 16, padB = 28;
  const innerW = W - padL - padR;
  const innerH = H - padT - padB;
  const yTicks = [0, 0.25, 0.5, 0.75, 1].map((t) => padT + t * innerH);
  const xTicks = [0, 0.25, 0.5, 0.75, 1].map((t) => padL + t * innerW);

  return (
    <svg viewBox={`0 0 ${W} ${H}`} style={{ width: '100%', display: 'block' }}>
      {yTicks.map((y, i) => (
        <line key={`y${i}`} x1={padL} x2={W - padR} y1={y} y2={y}
          stroke="var(--border)" strokeDasharray="3 5" strokeWidth="1" />
      ))}
      <line x1={padL} x2={W - padR} y1={padT + innerH} y2={padT + innerH}
        stroke="var(--border-strong)" strokeWidth="1" />
      {xTicks.map((x, i) => (
        <text key={`x${i}`} x={x} y={H - 9} textAnchor="middle" fill="var(--muted-2)"
          style={{ fontFamily: 'var(--font-mono)', fontSize: 11, letterSpacing: '0.04em' }}>
          —
        </text>
      ))}
      <text x={W / 2} y={padT + innerH / 2 + 4} textAnchor="middle" fill="var(--muted)"
        style={{ fontFamily: 'var(--font-sans)', fontSize: 12 }}>
        {message}
      </text>
    </svg>
  );
}

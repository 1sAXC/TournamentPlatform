interface Props {
  data: number[];
  labels: string[];
  trend?: 'up' | 'down' | 'flat';
}

export function EloChart({ data, labels, trend = 'flat' }: Props) {
  if (data.length === 0) {
    return (
      <div className="empty" style={{ padding: 24, fontSize: 11 }}>
        Нет данных
      </div>
    );
  }

  const W = 600, H = 200;
  const padL = 46, padR = 18, padT = 16, padB = 28;
  const innerW = W - padL - padR;
  const innerH = H - padT - padB;

  const min = Math.min(...data);
  const max = Math.max(...data);
  const range = max - min || 1;
  const niceStep = Math.max(20, Math.ceil(range / 3 / 10) * 10);
  const yMin = Math.floor(min / niceStep) * niceStep;
  const yMax = Math.ceil(max / niceStep) * niceStep || yMin + niceStep;
  const ticks: number[] = [];
  for (let v = yMin; v <= yMax; v += niceStep) ticks.push(v);

  const x = (i: number) =>
    padL + (data.length === 1 ? innerW / 2 : (i * innerW) / (data.length - 1));
  const y = (v: number) =>
    padT + innerH - ((v - yMin) / (yMax - yMin || 1)) * innerH;

  const linePts = data.map((v, i) => `${x(i)},${y(v)}`).join(' ');
  const areaPts = `${x(0)},${padT + innerH} ${linePts} ${x(data.length - 1)},${padT + innerH}`;

  const color = trend === 'up' ? 'var(--accent)'
    : trend === 'down' ? 'var(--danger)' : 'var(--muted)';
  const fillColor = trend === 'up' ? 'var(--accent-soft)'
    : trend === 'down' ? 'color-mix(in oklab, var(--danger), transparent 88%)'
      : 'transparent';

  const last = data[data.length - 1];

  return (
    <svg viewBox={`0 0 ${W} ${H}`} style={{ width: '100%', display: 'block' }}>
      {ticks.map((v) => (
        <g key={v}>
          <line x1={padL} x2={W - padR} y1={y(v)} y2={y(v)}
            stroke="var(--border)" strokeDasharray="3 5" strokeWidth="1" />
          <text x={padL - 8} y={y(v) + 4} textAnchor="end" fill="var(--muted-2)"
            style={{ fontFamily: 'var(--font-mono)', fontSize: 11 }}>{v}</text>
        </g>
      ))}
      <line x1={padL} x2={W - padR} y1={padT + innerH} y2={padT + innerH}
        stroke="var(--border-strong)" strokeWidth="1" />
      <polygon points={areaPts} fill={fillColor} stroke="none" />
      <polyline points={linePts} fill="none" stroke={color} strokeWidth="2"
        strokeLinejoin="round" strokeLinecap="round" />
      {data.map((v, i) => (
        <circle key={i} cx={x(i)} cy={y(v)} r={i === data.length - 1 ? 4 : 3}
          fill={i === data.length - 1 ? color : 'var(--surface-2)'}
          stroke={color} strokeWidth="1.5" />
      ))}
      {labels.map((m, i) => (
        <text key={i} x={x(i)} y={H - 9} textAnchor="middle" fill="var(--muted-2)"
          style={{ fontFamily: 'var(--font-mono)', fontSize: 11, letterSpacing: '0.04em' }}>
          {m}
        </text>
      ))}
      <g>
        <rect x={x(data.length - 1) - 28} y={y(last) - 24} width={56} height={17} rx={3}
          fill={color} opacity={0.22} />
        <text x={x(data.length - 1)} y={y(last) - 12} textAnchor="middle" fill={color}
          style={{ fontFamily: 'var(--font-mono)', fontSize: 11, fontWeight: 600 }}>{last}</text>
      </g>
    </svg>
  );
}

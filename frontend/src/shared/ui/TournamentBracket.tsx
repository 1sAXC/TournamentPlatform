export interface BracketMatch {
  id: string;
  label?: string;
  a?: string | null;
  b?: string | null;
  sa?: number | null;
  sb?: number | null;
  win?: 'a' | 'b' | null;
  status: 'pending' | 'done' | 'tbd';
  onClick?: () => void;
}

export interface BracketRound {
  label: string;
  current?: boolean;
  matches: BracketMatch[];
}

interface Props {
  rounds: BracketRound[];
  // 'fold' draws fold-style connectors assuming each round halves the previous
  //   (classic SE / DE upper bracket).
  // 'linear' just lays out columns evenly with no connectors — for tracks
  //   where round-to-round size doesn't follow halving (DE lower bracket
  //   absorption rounds, single-match Grand Final / 3rd-place tracks).
  layout?: 'fold' | 'linear';
}

export function TournamentBracket({ rounds, layout = 'fold' }: Props) {
  if (rounds.length === 0) return null;

  const matchH = 64;
  const matchGap = 20;
  const colW = 220;
  const colGap = 64;
  const headH = 32;
  const pad = 16;

  const maxCount = rounds.reduce((acc, r) => Math.max(acc, r.matches.length), 1);
  const innerH = maxCount * matchH + (maxCount - 1) * matchGap;
  const totalH = headH + innerH + pad * 2;
  const totalW = rounds.length * colW + (rounds.length - 1) * colGap + pad * 2;

  const positions: number[][] = [];
  if (layout === 'fold') {
    positions[0] = rounds[0].matches.map((_, i) => pad + headH + i * (matchH + matchGap) + matchH / 2);
    for (let r = 1; r < rounds.length; r++) {
      positions[r] = rounds[r].matches.map((_, i) => {
        const p0 = positions[r - 1][i * 2];
        const p1 = positions[r - 1][i * 2 + 1] ?? p0;
        return (p0 + p1) / 2;
      });
    }
  } else {
    // Linear: each column's matches are evenly spaced and vertically centered
    // around the section height. No connectors get drawn.
    for (let r = 0; r < rounds.length; r++) {
      const n = rounds[r].matches.length;
      const colH = n * matchH + (n - 1) * matchGap;
      const top = pad + headH + (innerH - colH) / 2;
      positions[r] = rounds[r].matches.map((_, i) => top + i * (matchH + matchGap) + matchH / 2);
    }
  }

  const xLeft = (r: number) => pad + r * (colW + colGap);
  const xRight = (r: number) => xLeft(r) + colW;

  return (
    <div className="bracket-wrap">
      <div className="bracket-canvas" style={{ width: totalW, height: totalH }}>
        <svg width={totalW} height={totalH} className="bracket-svg">
          <defs>
            <pattern id="bracket-grid" width="40" height="40" patternUnits="userSpaceOnUse">
              <path d="M40 0H0V40" fill="none" stroke="currentColor" strokeWidth="0.5" opacity="0.4" />
            </pattern>
          </defs>
          <rect width={totalW} height={totalH} fill="url(#bracket-grid)" color="var(--border)" />

          {layout === 'fold' && rounds.slice(1).map((round, rIdx) => {
            const r = rIdx + 1;
            return round.matches.map((m, i) => {
              const childY = positions[r][i];
              const p0 = positions[r - 1][i * 2];
              const p1 = positions[r - 1][i * 2 + 1];
              const startX = xRight(r - 1);
              const endX = xLeft(r);
              const midX = (startX + endX) / 2;
              if (p1 == null) {
                return (
                  <path key={`c-${r}-${i}`}
                    d={`M${startX},${p0} L${endX},${childY}`}
                    stroke="var(--border-strong)" strokeWidth="1" fill="none"
                  />
                );
              }
              const winLine = m.win === 'a' || m.win === 'b';
              const lineCol = winLine ? 'var(--accent-line)' : 'var(--border-strong)';
              return (
                <g key={`c-${r}-${i}`}>
                  <path
                    d={`M${startX},${p0} L${midX},${p0} L${midX},${p1} L${startX},${p1}`}
                    stroke={lineCol} strokeWidth="1" fill="none"
                  />
                  <path
                    d={`M${midX},${childY} L${endX},${childY}`}
                    stroke={lineCol} strokeWidth="1" fill="none"
                  />
                  <circle cx={midX} cy={childY} r="2" fill={winLine ? 'var(--accent)' : 'var(--muted-2)'} />
                </g>
              );
            });
          })}
        </svg>

        {rounds.map((round, r) => (
          <div
            key={`lbl-${r}`}
            className={`bracket-round-label ${round.current ? 'current' : ''}`}
            style={{ left: xLeft(r), top: pad, width: colW }}
          >
            <span>{round.label}</span>
            {round.current && <span className="dot" />}
          </div>
        ))}

        {rounds.map((round, r) =>
          round.matches.map((m, i) => (
            <div
              key={`m-${r}-${i}`}
              className={`b-box ${m.status === 'pending' ? 'pending' : ''} ${m.onClick ? 'clickable' : ''}`}
              style={{
                left: xLeft(r),
                top: positions[r][i] - matchH / 2,
                width: colW,
                height: matchH,
              }}
              onClick={m.onClick}
            >
              <div className="b-box-head">
                <span className="b-box-id">{m.label ?? m.id}</span>
                {m.status === 'pending' && <span className="b-box-meta pending">в очереди</span>}
                {m.status === 'done' && <span className="b-box-meta done">DONE</span>}
              </div>
              <div className={`b-team ${m.win === 'a' ? 'win' : m.win === 'b' ? 'loss' : m.status === 'pending' ? 'pending' : ''}`}>
                <span className="name">{m.a || '—'}</span>
                <span className="score">{m.sa ?? '·'}</span>
              </div>
              <div className={`b-team ${m.win === 'b' ? 'win' : m.win === 'a' ? 'loss' : m.status === 'pending' ? 'pending' : ''}`}>
                <span className="name">{m.b || '—'}</span>
                <span className="score">{m.sb ?? '·'}</span>
              </div>
            </div>
          )),
        )}
      </div>
    </div>
  );
}

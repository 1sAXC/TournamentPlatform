import { TournamentBracket } from '@/shared/ui/TournamentBracket';
import type { BracketSection } from '@/shared/lib/bracket';

// Renders the full bracket viewer for a tournament. Each section is shown
// as its own horizontal strip with an optional heading. Sections are stacked
// vertically — for Double Elimination this gives the canonical layout:
// Upper bracket on top, Lower bracket below, Grand Final at the bottom.
export function TournamentBracketView({ sections }: { sections: BracketSection[] }) {
  if (sections.length === 0) return null;
  return (
    <div className="col" style={{ gap: 16 }}>
      {sections.map((s, i) => (
        <div key={i} className="col" style={{ gap: 8 }}>
          {s.title && (
            <div
              style={{
                fontSize: 12,
                fontWeight: 600,
                color: 'var(--muted)',
                textTransform: 'uppercase',
                letterSpacing: 0.5,
              }}
            >
              {s.title}
            </div>
          )}
          <TournamentBracket rounds={s.rounds} layout={s.layout} />
        </div>
      ))}
    </div>
  );
}

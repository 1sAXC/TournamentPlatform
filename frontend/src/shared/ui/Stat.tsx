interface Props {
  label: string;
  value: string | number;
  delta?: { text: string; dir?: 'up' | 'dn' };
}

export function Stat({ label, value, delta }: Props) {
  return (
    <div className="stat">
      <div className="lbl">{label}</div>
      <div className="val">{value}</div>
      {delta && <div className={`delta ${delta.dir || ''}`}>{delta.text}</div>}
    </div>
  );
}

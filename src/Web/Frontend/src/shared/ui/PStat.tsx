interface Props {
  value: string | number;
  label: string;
  tone?: 'accent' | 'success' | 'warning';
}

export function PStat({ value, label, tone }: Props) {
  return (
    <div className="pstat">
      <div className={`pstat-val ${tone || ''}`}>{value}</div>
      <div className="pstat-lbl">{label}</div>
    </div>
  );
}

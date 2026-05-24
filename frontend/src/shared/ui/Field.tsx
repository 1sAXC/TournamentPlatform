import type { ReactNode } from 'react';

interface Props {
  label?: ReactNode;
  hint?: ReactNode;
  error?: ReactNode;
  children: ReactNode;
}

export function Field({ label, hint, error, children }: Props) {
  return (
    <div className="field">
      {label && <label className="label">{label}</label>}
      {children}
      {(hint || error) && (
        <div className={`helper ${error ? 'hint-error' : ''}`}>{error || hint}</div>
      )}
    </div>
  );
}

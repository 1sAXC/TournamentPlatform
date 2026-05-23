import type { ReactNode } from 'react';
import { Icon } from './Icon';

interface Props {
  title?: ReactNode;
  eyebrow?: ReactNode;
  onClose: () => void;
  children: ReactNode;
  footer?: ReactNode;
  width?: number;
}

export function Modal({ title, eyebrow, onClose, children, footer, width }: Props) {
  return (
    <div className="modal-back" onClick={onClose}>
      <div className="modal" style={{ minWidth: width || 460 }} onClick={(e) => e.stopPropagation()}>
        <div className="modal-head">
          <div className="col" style={{ gap: 2, flex: 1 }}>
            {eyebrow && <div className="eyebrow">{eyebrow}</div>}
            {title && <h3 style={{ fontSize: 14 }}>{title}</h3>}
          </div>
          <button className="btn btn-ghost btn-sm" onClick={onClose} aria-label="Закрыть">
            <Icon name="x" size={12} />
          </button>
        </div>
        <div className="modal-body">{children}</div>
        {footer && <div className="modal-foot">{footer}</div>}
      </div>
    </div>
  );
}

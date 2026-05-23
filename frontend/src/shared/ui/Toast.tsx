import { create } from 'zustand';
import { Icon } from './Icon';

export type ToastKind = 'info' | 'success' | 'error';

interface ToastItem { id: number; kind: ToastKind; message: string; }

interface ToastState {
  items: ToastItem[];
  push: (kind: ToastKind, message: string) => void;
  dismiss: (id: number) => void;
}

let counter = 0;
export const useToasts = create<ToastState>((set) => ({
  items: [],
  push: (kind, message) => {
    const id = ++counter;
    set((s) => ({ items: [...s.items, { id, kind, message }] }));
    setTimeout(() => set((s) => ({ items: s.items.filter(t => t.id !== id) })), 4500);
  },
  dismiss: (id) => set((s) => ({ items: s.items.filter(t => t.id !== id) })),
}));

export function showToast(kind: ToastKind, message: string) {
  useToasts.getState().push(kind, message);
}

export function ToastStack() {
  const items = useToasts((s) => s.items);
  const dismiss = useToasts((s) => s.dismiss);
  if (items.length === 0) return null;
  return (
    <div className="toast-stack">
      {items.map(t => (
        <div key={t.id} className={`toast ${t.kind}`}>
          <span style={{ flex: 1 }}>{t.message}</span>
          <button className="btn btn-ghost btn-sm" onClick={() => dismiss(t.id)} aria-label="Закрыть">
            <Icon name="x" size={11} />
          </button>
        </div>
      ))}
    </div>
  );
}

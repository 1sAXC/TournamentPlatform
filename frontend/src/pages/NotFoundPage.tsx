import { Link } from 'react-router-dom';

export function NotFoundPage() {
  return (
    <div style={{
      width: '100%', height: '100%',
      display: 'grid', placeItems: 'center', padding: 24,
    }}>
      <div className="card card-pad" style={{ textAlign: 'center', maxWidth: 380 }}>
        <div className="eyebrow" style={{ marginBottom: 8 }}>404</div>
        <h1 style={{ fontSize: 22, marginBottom: 8 }}>Страница не найдена</h1>
        <p style={{ color: 'var(--muted)', fontSize: 13, marginBottom: 16 }}>
          Возможно, ссылка устарела или вы ввели неверный адрес.
        </p>
        <Link to="/" className="btn btn-primary">На главную</Link>
      </div>
    </div>
  );
}

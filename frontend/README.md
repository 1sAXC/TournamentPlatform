# Tournament Platform — Frontend

React + Vite + TypeScript SPA для платформы турниров. Подключается к микросервисам Auth/Tournament/Rating через nginx reverse proxy. Располагается рядом с .NET-решением: `frontend/` в корне репозитория.

## Стек

- React 18 + TypeScript
- Vite 5
- TanStack Query 5 (серверное состояние)
- Zustand (auth-store)
- React Router 6
- react-hook-form + zod
- axios
- CSS Modules + дизайн-токены из мокапа

## Архитектура

```
src/
├── app/             # router, queryClient, RoleHome
├── shared/
│   ├── api/         # http (axios + interceptors) + *Api модули + types.ts
│   ├── auth/        # authStore (Zustand), ProtectedRoute, GuestRoute
│   ├── lib/         # jwt, formatters, disciplines, initials
│   ├── styles/      # tokens.css, reset.css, global.css
│   └── ui/          # Button/Card/Field/Badge/Avatar/Modal/Toast/Bracket…
├── features/        # хуки по доменам (useTournaments, useAdminUsers…)
└── pages/           # экраны 1:1 с мокапом
    ├── guest/       # Login, RegisterPlayer, RegisterOrganizer
    ├── player/      # Home, Catalog, Detail, MyTournaments, Profile
    ├── organizer/   # Tournaments, Create, Manage, Profile, Pending
    └── admin/       # Users, Applications, Tournaments
```

Слои: `pages` → `features` (хуки + формы) → `shared/api` + `shared/ui`.

## Локальная разработка

```sh
npm install
npm run dev
```

Vite поднимет дев-сервер на `http://localhost:3000` и проксирует:
- `/api/auth/*` → `localhost:5240` (Auth.Api)
- `/api/admin/tournaments/*` → `localhost:5015` (Tournament.Api)
- `/api/admin/*` → `localhost:5240`
- `/api/tournaments/*`, `/api/organizer/*` → `localhost:5015`
- `/api/ratings/*` → `localhost:5196` (Rating.Api)

Сначала поднимите бэк: `docker compose up auth-api tournament-api rating-api` из корня репозитория.

## Сборка / типы

```sh
npm run build      # tsc + vite build → dist/
npm run typecheck  # только tsc, без эмита
npm run preview    # локальный сервер для dist
```

## Запуск в Docker (вместе со всем стеком)

Из корня репозитория:

```sh
docker compose up -d --build
```

После этого:
- `http://localhost:3000` — фронт
- `http://localhost:5240/swagger` — Auth.Api
- `http://localhost:5015/swagger` — Tournament.Api
- `http://localhost:5196/swagger` — Rating.Api
- `http://localhost:15672` — RabbitMQ UI

## JWT

Токен и пользователь хранятся в `localStorage` (`tp.token`, `tp.user`). На 401-ответе или истёкшем токене axios-интерцептор сбрасывает стор и редиректит на `/login`. При первом монтировании `App` вызывает `hydrate()` — читает `localStorage`, проверяет `exp`, и либо сетит state, либо чистит.

## Маршруты

| Путь | Роль |
|---|---|
| `/login`, `/register/player`, `/register/organizer` | гость |
| `/` | диспатчер по роли |
| `/home`, `/tournaments`, `/tournaments/:id`, `/my-tournaments`, `/profile` | Player |
| `/organizer`, `/organizer/create`, `/organizer/tournaments/:id` | ActiveOrganizer |
| `/organizer/profile`, `/organizer/pending` | Organizer (любой статус) |
| `/admin/applications`, `/admin/users`, `/admin/tournaments` | Admin |

## Что не вошло в этот этап

- Pagination для каталога турниров (UI есть, в API его нет — список приходит целиком).
- Причина отклонения заявки организатора хранится только локально — бэк сейчас не принимает payload в `/reject`.
- Realtime: используется ручной refetch + invalidation после мутаций (SignalR/SSE не используется).

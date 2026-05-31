# Tournament Platform

Платформа для проведения киберспортивных турниров: регистрация игроков и организаторов, создание турниров, автоматическое формирование сеток (Single/Double Elimination, Swiss), ведение рейтинга ELO, уведомления о матчах и контакты для связи между капитанами.

Проект построен как набор микросервисов на **.NET 8** с фронтендом на **React + TypeScript**. Сервисы общаются через **RabbitMQ** (асинхронные интеграционные события с inbox/outbox для надёжной доставки), у каждого собственная **PostgreSQL** база и собственный API. На фронт ходит через **nginx**, который проксирует запросы в нужный сервис по префиксу `/api/...`.

## Микросервисы

| Сервис | Назначение | Контейнер | Порт хоста |
|---|---|---|---|
| `auth-api` | Регистрация, логин, выдача JWT, профиль, заявки организаторов, админка пользователей | `tournamentplatform-auth-api` | **5240** |
| `tournament-api` | Турниры, регистрация участников, балансировка команд, генерация сеток, матчи | `tournamentplatform-tournament-api` | **5015** |
| `rating-api` | Рейтинг ELO по дисциплинам, история изменений, реакция на завершение матчей | `tournamentplatform-rating-api` | **5196** |
| `notification-api` | Уведомления игрокам о сформированных матчах | `tournamentplatform-notification-api` | **5210** |
| `frontend` | SPA на React + nginx (раздача статики и прокси `/api/*`) | `tournamentplatform-frontend` | **3000** |
| `rabbitmq` | Шина интеграционных событий | `tournamentplatform-rabbitmq` | **5672** (AMQP), **15672** (UI) |
| `auth-db` | PostgreSQL | `tournamentplatform-auth-db` | **5433** |
| `tournament-db` | PostgreSQL | `tournamentplatform-tournament-db` | **5434** |
| `rating-db` | PostgreSQL | `tournamentplatform-rating-db` | **5435** |
| `notification-db` | PostgreSQL | `tournamentplatform-notification-db` | **5436** |

После старта стека веб-приложение доступно по адресу [http://localhost:3000](http://localhost:3000). Управление RabbitMQ — [http://localhost:15672](http://localhost:15672) (логин/пароль `guest` / `guest`).

## Запуск

### Требования
- Docker и Docker Compose
- Свободные порты 3000, 5015, 5196, 5210, 5240, 5433–5436, 5672, 15672

### Первый запуск

1. Создать файл `.env` рядом с `docker-compose.yml`:
   ```env
   JWT_SECRET=please-replace-this-with-a-32-plus-character-secret
   ```
   Это секрет для подписи JWT — обязателен и должен быть длиной ≥ 32 символа. Один и тот же для всех сервисов, иначе они не смогут проверять токены друг друга.

2. Поднять стек:
   ```bash
   docker compose up -d --build
   ```
   Первый запуск займёт несколько минут — собираются образы .NET-сервисов и фронт. Контейнеры с миграциями автоматически создают схему БД при старте.

3. Открыть [http://localhost:3000](http://localhost:3000).

### Пересборка после изменений

Если правил код:
```bash
docker compose up -d --build <имя_сервиса>
```
например, `docker compose up -d --build tournament-api frontend`. Compose сам разрешит зависимости.

Если нужно полностью пересоздать БД (например, при изменении схемы и сбросе тестовых данных):
```bash
docker compose down -v
docker compose up -d --build
```
> ⚠️ `-v` удаляет тома `auth-db-data`, `tournament-db-data`, `rating-db-data`, `notification-db-data` — все локальные данные пропадут.

## Архитектура

### Связь сервисов

```
                        ┌──────────────┐
                        │   RabbitMQ   │
                        └──┬──┬──┬──┬──┘
                ┌──────────┘  │  │  └──────────┐
                ▼             ▼  ▼             ▼
        ┌──────────┐    ┌──────────┐    ┌──────────────┐
        │ auth-api │◄──►│tournament│◄──►│ rating-api   │
        └────┬─────┘    │  -api    │    └──────┬───────┘
             │          └────┬─────┘           │
             │               │                 │
             ▼               ▼                 ▼
        ┌─────────┐     ┌──────────┐     ┌──────────┐
        │ auth-db │     │tournament│     │ rating-db│
        └─────────┘     │   -db    │     └──────────┘
                        └──────────┘
                              ▲
                              │ событие RoundCreated
                              ▼
                  ┌─────────────────────┐
                  │  notification-api   │ ─── notification-db
                  └─────────────────────┘
                              ▲
                              │ /api/notifications
                  ┌─────────────────────┐
                  │ frontend (nginx)    │ ─── порт 3000
                  └─────────────────────┘
```

### Интеграционные события через шину

Все межсервисные взаимодействия — асинхронные, через RabbitMQ. Каждый сервис при изменении состояния пишет событие в **outbox-таблицу** в той же транзакции, фоновый воркер публикует его в шину. Потребители обрабатывают сообщения с дедупликацией через **inbox-таблицу** — повторная доставка одного и того же `EventId` игнорируется.

Ключевые события:

| Событие | Источник | Кто слушает | Эффект |
|---|---|---|---|
| `UserCreatedEvent` | `auth-api` | `tournament-api`, `rating-api` | Создание проекций пользователя (роль, никнейм, contact handle, начальные рейтинги). |
| `UserContactHandleChangedEvent` | `auth-api` | `tournament-api` | Обновление `UserProjection.ContactHandle` для актуальных контактов на странице матча. |
| `UserBlockedEvent`, `UserRoleChangedEvent` | `auth-api` | `tournament-api`, `rating-api` | Синхронизация состояния проекций. |
| `RoundCreatedEvent` | `tournament-api` | `notification-api` | На каждый матч раунда — фан-аут уведомления членам команд. |
| `MatchCompletedEvent` | `tournament-api` | `rating-api` | Обновление ELO с учётом разрыва по раундам. |
| `RatingUpdatedEvent` | `rating-api` | `tournament-api` | Обновление `PlayerRatingProjection` в Tournament-сервисе. |
| `TournamentCompletedEvent` | `tournament-api` | (расширяемо) | Сигнал о завершении турнира. |

### Проекции вместо синхронных вызовов

`tournament-api` хранит локальные проекции данных, принадлежащих другим сервисам:
- **`UserProjection`** — отзеркаливает роль и `ContactHandle` пользователя из Auth. Это позволяет странице матча возвращать контакты участников без HTTP-вызова в `auth-api`.
- **`PlayerRatingProjection`** — отзеркаливает ELO из Rating. Используется для балансировки команд при формировании.

Проекции обновляются через события (`UserCreatedEvent`, `UserContactHandleChangedEvent`, `RatingUpdatedEvent` и др.). При недоступности сервиса-источника данные остаются актуальными на момент последнего успешного события — на странице матча всё ещё видно последнее известное состояние.

## Ключевые доменные решения

### Дисциплины
Платформа поддерживает только **раундовые шутеры**: **CS2**, **Valorant**, **Standoff 2**. Это сделано осознанно — модель счёта матча (см. ниже) опирается на «раунды внутри карты», что не подходит для дисциплин без раундовой структуры (Dota 2, LoL, PUBG).

### Счёт матча: карты + раунды
Каждая запись завершённого матча хранит **две системы счёта**:
- **`WinnerMaps` / `LoserMaps`** — карты, выигранные в серии (например, `2 / 1` для Bo3). Это «итог» серии, который видят игроки в карточках матча и в турнирной сетке.
- **`WinnerScore` / `LoserScore`** — раунды, суммарно по всем картам серии (например, `26 / 20` для `13-9 + 13-11`). Эти числа подаются в Rating-сервис как input для коэффициента уверенности победы в ELO.

Валидация требует **внутренней согласованности**: победитель серии по картам не может проиграть по сумме раундов, иначе результат отклоняется как неконсистентный.

Такое разделение устраняет неоднозначность раннего прототипа, где одно поле «счёт» использовалось то для карт, то для раундов в разных контекстах, что приводило к некорректному обновлению ELO.

### ELO
- Базовая формула Эло с K-фактором, зависящим от размера команды.
- **Score-coefficient**: `1 + min(|winnerRounds − loserRounds|, 10) · 0.025` — чем больше разрыв в раундах, тем сильнее обновляется рейтинг (cap `+25%` при разрыве ≥10).
- Технические поражения дают максимальный коэффициент.
- Подробнее: [`RatingService.cs`](src/Services/Rating/Rating.Application/Ratings/Services/RatingService.cs).

### Регистрация и контакт для связи
При регистрации игрока и организатора **обязательно** указывается `ContactHandle` (Telegram, Discord и т.п., 1–64 символа). Этот контакт виден участникам матча и организатору на странице матча — капитаны связываются между собой вне платформы и согласовывают время. Контакт можно изменить в профиле в любой момент; событие `UserContactHandleChangedEvent` обновит проекцию в Tournament-сервисе.

### Авторизация заявок организаторов
Регистрация игрока — мгновенная: аккаунт сразу `Active`. Регистрация организатора — заявка со статусом `PendingApproval`. Заявка попадает в очередь админа (`/admin/applications`), где её можно одобрить или отклонить. До одобрения организатор может только зайти в профиль и увидеть свой статус.

### Уведомления
Игрок получает уведомление, когда формируется матч с его участием. Триггер — событие `RoundCreatedEvent`, которое `tournament-api` эмитит при каждом создании раунда (стартовый раунд любого формата + автогенерируемые в SE/DE + ручной «Следующий раунд» в Swiss).

Notification-сервис делает per-match per-recipient фан-аут: для каждого матча в раунде — на каждого члена обеих команд создаётся запись `Notification` с ссылкой `/tournaments/{tid}/matches/{mid}`. Идемпотентность гарантирована inbox-таблицей в `notification-db` + уникальным индексом `(SourceEventId, RecipientUserId)`.

Фронт через хук `useNotifications` опрашивает `/api/notifications` раз в 30 секунд (опрос синхронизирован с `useMeSync`, который тем же ритмом следит за статусом аккаунта). При возврате на вкладку — обновляется сразу (`refetchOnWindowFocus: true`). При клике в дропдауне колокольчика отметка «прочитано» инвалидирует кеш — счётчик мгновенно перерисовывается, не дожидаясь следующего тика.

## Тесты

```bash
dotnet test tests/Auth.Tests/Auth.Tests.csproj
dotnet test tests/Tournament.Tests/Tournament.Tests.csproj
dotnet test tests/Rating.Tests/Rating.Tests.csproj
dotnet test tests/Notification.Tests/Notification.Tests.csproj
```

Текущая статистика (после фазы 8.5):
- **Auth.Tests**: 47 тестов
- **Tournament.Tests**: 95 тестов
- **Rating.Tests**: 18 тестов
- **Notification.Tests**: 10 тестов

`Integration.Tests` присутствует в репозитории, но не собирается актуально — будет восстановлен в отдельной задаче.

## Структура репозитория

```
src/
  BuildingBlocks/
    TournamentPlatform.Contracts/    — общие DTO интеграционных событий
    TournamentPlatform.Messaging/    — RabbitMQ, outbox/inbox инфраструктура
    TournamentPlatform.Shared/       — общие утилиты (Result<>, JWT helpers, etc.)
  Services/
    Auth/                            — Auth.{Api,Application,Domain,Infrastructure}
    Tournament/                      — Tournament.{Api,Application,Domain,Infrastructure}
    Rating/                          — Rating.{Api,Application,Domain,Infrastructure}
    Notification/                    — Notification.{Api,Application,Domain,Infrastructure}
tests/
  Auth.Tests/
  Tournament.Tests/
  Rating.Tests/
  Notification.Tests/
frontend/
  src/
    app/             — роутинг, query client
    features/        — feature hooks (auth, tournaments, ratings, notifications)
    pages/           — страницы по ролям (guest/player/organizer/admin/match/notifications)
    shared/
      api/           — http клиент, типы, API клиенты
      auth/          — auth store, ProtectedRoute, GuestRoute
      lib/           — утилиты (formatters, matchScore, disciplines, ...)
      ui/            — переиспользуемые компоненты
  nginx.conf         — прокси /api/* → нужный микросервис
docker-compose.yml
```

## Сценарий «капитан-к-капитану» (целевой UX)

1. Игроки `A` и `B` регистрируются на платформе с обязательным контактом (например, `@captain_a` и `@captain_b` в Telegram).
2. Организатор регистрируется, админ одобряет заявку.
3. Организатор создаёт турнир по CS2 в формате Swiss или Single Elimination, оба игрока подают заявку.
4. Организатор стартует турнир → `tournament-api` формирует первый раунд → выбрасывает `RoundCreatedEvent`.
5. `notification-api` подбирает событие, создаёт уведомления для обоих игроков.
6. Игрок `A`, заходя на сайт (или находясь на нём), видит цифру `1` на колокольчике в шапке. Клик по уведомлению `«Сформирован матч в турнире "..."»` → попадает на страницу матча `/tournaments/{tid}/matches/{mid}`.
7. На странице матча игрок `A` видит свою команду, команду соперника, капитана соперника (бейдж «Капитан»), его контакт `@captain_b`, контакт организатора, описание турнира.
8. Капитаны связываются в Telegram, договариваются о времени матча.
9. После игры организатор вносит результат (карты `2-1` + раунды `26-20`) → `tournament-api` эмитит `MatchCompletedEvent` → `rating-api` пересчитывает ELO.

## Ссылки на ключевые файлы

- Доменная модель турнира: [`Tournament.cs`](src/Services/Tournament/Tournament.Domain/Tournaments/Tournament.cs)
- Генераторы сеток: [`Brackets/`](src/Services/Tournament/Tournament.Application/Brackets)
- Формула ELO: [`RatingService.cs`](src/Services/Rating/Rating.Application/Ratings/Services/RatingService.cs)
- Фан-аут уведомлений: [`RoundCreatedFanout.cs`](src/Services/Notification/Notification.Application/Notifications/Services/RoundCreatedFanout.cs)
- Страница матча на фронте: [`MatchDetailPage.tsx`](frontend/src/pages/match/MatchDetailPage.tsx)
- Колокольчик уведомлений: [`NotificationBell.tsx`](frontend/src/shared/ui/NotificationBell.tsx)

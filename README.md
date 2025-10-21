# Data Searcher

Микросервисная платформа для быстрой индексации и поиска по разнородным таблицам данных. Решение состоит из двух C#-сервисов и веб-интерфейса на React + Ant Design, использует OpenSearch в качестве движка полнотекстового поиска.

## Архитектура

```
┌────────────────────┐      ┌──────────────────────┐
│ Search UI (React)  │◀────▶│ Search Service (C#)  │
└────────────────────┘      └──────────────────────┘
             ▲                        │
             │                        ▼
┌────────────────────┐      ┌──────────────────────┐
│ Data ingestion API │────▶│ OpenSearch cluster    │
└────────────────────┘      └──────────────────────┘
```

- **DataIngestionService** — принимает CSV/JSON файлы и ручные записи, нормализует их и индексирует в OpenSearch.
- **SearchService** — предоставляет REST API для полнотекстового поиска, фильтрации и сортировки.
- **Search UI** — SPA на React/Ant Design для работы с поиском, загрузкой файлов и ручным вводом данных.

Все сервисы используют общий индекс `records`, а OpenSearch настраивается динамически для поддержки произвольных полей.

## Каталоги

| Путь | Описание |
| ---- | -------- |
| `backend/DataIngestionService` | API для загрузки данных |
| `backend/SearchService` | API для полнотекстового поиска |
| `backend/Shared/Records` | Общие модели и конфигурация |
| `frontend/search-ui` | Веб-интерфейс на React + Ant Design |
| `deploy` | Скрипты деплоя (Docker Compose и Kubernetes) |

## Быстрый старт (Docker Compose)

```bash
cd deploy
docker compose up --build
```

После запуска UI доступен по адресу `http://localhost:3000`.

## Разработка

### Backend

```bash
# Сервис загрузки данных
cd backend/DataIngestionService
dotnet restore
dotnet run

# Сервис поиска
cd backend/SearchService
dotnet restore
dotnet run
```

Перед запуском убедитесь, что OpenSearch доступен (например, через Docker Compose).

### Frontend

```bash
cd frontend/search-ui
npm install
npm run dev
```

Dev-сервер откроется на `http://localhost:5173`.

## Деплой

Подробные инструкции находятся в [deploy/README.md](deploy/README.md).

- `docker-compose` для локального окружения
- Kubernetes-манифесты для namespace `data-search`

## API

Каждый сервис публикует Swagger UI (`/swagger`). Основные эндпоинты:

- `POST /api/datasets/{dataset}/records` — ручное добавление записи
- `POST /api/datasets/{dataset}/upload` — загрузка CSV/JSON файла
- `GET /api/search` — поиск с фильтрацией и сортировкой
- `GET /api/datasets` — список датасетов

## Оптимизация и масштабирование

- Индексация выполняется батчами по 1000 записей, что позволяет обрабатывать миллионы строк.
- OpenSearch настраивается с dynamic templates для произвольных полей и keyword-представления для сортировки.
- Предусмотрены readiness/liveness probes и рекомендации по Horizontal Pod Autoscaler для Kubernetes.

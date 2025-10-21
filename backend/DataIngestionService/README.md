# Data Ingestion Service

Сервис отвечает за загрузку данных из CSV/JSON файлов и ручное добавление записей в индекс OpenSearch.

## Основные возможности

- Приём файлов CSV или JSON и потоковая индексация данных в OpenSearch
- Ручное добавление отдельных записей через API
- Автоматическая подготовка индекса с динамическими полями
- Swagger UI по адресу `/swagger`

## Переменные окружения

| Переменная | Описание | Значение по умолчанию |
| ---------- | -------- | --------------------- |
| `OpenSearch__Uri` | URI к узлу OpenSearch | `http://localhost:9200` |
| `OpenSearch__IndexName` | Имя индекса для хранения записей | `records` |
| `OpenSearch__Username` | Имя пользователя для Basic Auth (опционально) | – |
| `OpenSearch__Password` | Пароль для Basic Auth (опционально) | – |
| `OpenSearch__DisableCertificateValidation` | Отключить проверку SSL-сертификата | `true` |
| `ASPNETCORE_URLS` | Порт прослушивания | `http://0.0.0.0:8080` |

## Локальный запуск

```bash
# Установка зависимостей
cd backend/DataIngestionService
dotnet restore

# Запуск
dotnet run
```

После запуска API будет доступно по адресу `http://localhost:8080`.

## Основные эндпоинты

- `POST /api/datasets/{dataset}/records` — ручное добавление записи
- `POST /api/datasets/{dataset}/upload` — загрузка CSV/JSON файла
- `GET /health` — проверка статуса сервиса

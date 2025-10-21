# Search Service

REST API для выполнения полнотекстового поиска по агрегированным записям в OpenSearch.

## Возможности

- Поиск по произвольной строке с учетом всех полей записи
- Фильтрация по имени датасета
- Серверная пагинация и сортировка по score, дате создания, названию датасета и любому пользовательскому полю
- Получение списка доступных датасетов
- Swagger UI по адресу `/swagger`

## Переменные окружения

| Переменная | Описание | Значение по умолчанию |
| ---------- | -------- | --------------------- |
| `OpenSearch__Uri` | URI OpenSearch | `http://localhost:9200` |
| `OpenSearch__IndexName` | Имя индекса | `records` |
| `OpenSearch__Username` | Логин для Basic Auth (опционально) | – |
| `OpenSearch__Password` | Пароль для Basic Auth (опционально) | – |
| `OpenSearch__DisableCertificateValidation` | Отключить проверку сертификата | `true` |
| `ASPNETCORE_URLS` | Порт сервиса | `http://0.0.0.0:8081` |

## Запуск

```bash
cd backend/SearchService
dotnet restore
dotnet run
```

Основной эндпоинт: `GET /api/search`

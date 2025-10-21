# Деплой

## Docker Compose

```bash
cd deploy
docker compose up --build
```

После запуска:

- OpenSearch: `http://localhost:9200`
- Data Ingestion Service: `http://localhost:8080`
- Search Service: `http://localhost:8081`
- Web UI: `http://localhost:3000`

### Полезные переменные окружения

- `OpenSearch__IndexName` — имя индекса (по умолчанию `records`)
- `VITE_SEARCH_API_URL` — адрес сервиса поиска для UI
- `VITE_INGESTION_API_URL` — адрес сервиса загрузки данных для UI

## Kubernetes

1. Соберите и загрузите образы (пример для kind/minikube):

```bash
docker build -t data-ingestion-service:latest -f backend/DataIngestionService/Dockerfile .
docker build -t data-search-service:latest -f backend/SearchService/Dockerfile .
docker build -t data-search-frontend:latest -f frontend/search-ui/Dockerfile .
```

2. Примените манифесты:

```bash
kubectl apply -f deploy/k8s/namespace.yaml
kubectl apply -f deploy/k8s/opensearch.yaml
kubectl apply -f deploy/k8s/ingestion.yaml
kubectl apply -f deploy/k8s/search.yaml
kubectl apply -f deploy/k8s/frontend.yaml
```

3. Получите адрес UI:

```bash
kubectl get svc -n data-search search-frontend
```

> Для локального кластера (minikube/kind) используйте `kubectl port-forward svc/search-frontend 3000:80 -n data-search`.

### Настройка масштабирования

- Включите Horizontal Pod Autoscaler: `kubectl autoscale deployment search-service --cpu-percent=70 --min=1 --max=5 -n data-search`
- Используйте `kubectl get hpa -n data-search` для мониторинга

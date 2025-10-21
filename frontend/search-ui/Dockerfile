# syntax=docker/dockerfile:1
FROM node:18-alpine AS build
WORKDIR /app

COPY frontend/search-ui/package.json frontend/search-ui/package-lock.json* ./
RUN npm install

COPY frontend/search-ui/ ./

ARG VITE_SEARCH_API_URL=http://search-service:8081
ARG VITE_INGESTION_API_URL=http://ingestion-service:8080
ENV VITE_SEARCH_API_URL=$VITE_SEARCH_API_URL
ENV VITE_INGESTION_API_URL=$VITE_INGESTION_API_URL
RUN npm run build

FROM nginx:1.25-alpine AS final
COPY frontend/search-ui/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]

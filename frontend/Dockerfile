# ============================================================================
# MULTI-STAGE BUILD - Frontend Production Docker Image
# 
# Aşama 1: Node.js'de npm build komutunu çalıştır
# Aşama 2: Nginx ile production'a hazırlanan dosyaları serve et
# ============================================================================

# ============================================================================
# AŞAMA 1: BUILD STAGE (npm run build)
# ============================================================================
FROM node:18-alpine AS build-stage

WORKDIR /app

# package.json ve package-lock.json kopyala
COPY package*.json ./

# Dependencies kur
RUN npm install --legacy-peer-deps

# Tüm dosyaları kopyala
COPY . .

# Production build yap
RUN npm run build

# ============================================================================
# AŞAMA 2: PRODUCTION STAGE (Nginx)
# ============================================================================
FROM nginx:stable-alpine

WORKDIR /usr/share/nginx/html

# Build stage'den derlenmiş dosyaları kopyala
COPY --from=build-stage /app/build .

# API backend hostname - docker-compose'dan geçirilir
ARG API_HOST=ecommerce-api-prod
ENV API_HOST=${API_HOST}

# Nginx konfigürasyonu - React SPA + API Proxy
RUN echo 'server { \
    listen 80; \
    root /usr/share/nginx/html; \
    index index.html; \
    \
    # Maksimum upload boyutu: 10MB \
    client_max_body_size 10M; \
    \
    # API Proxy - Backend'e yönlendir \
    location /api { \
        proxy_pass http://'"${API_HOST}"':5000; \
        proxy_http_version 1.1; \
        proxy_set_header Host $host; \
        proxy_set_header X-Real-IP $remote_addr; \
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for; \
        proxy_set_header X-Forwarded-Proto $scheme; \
        proxy_read_timeout 300s; \
        proxy_connect_timeout 75s; \
        proxy_request_buffering off; \
        proxy_buffering off; \
    } \
    \
    # Uploads Proxy \
    location /uploads { \
        proxy_pass http://'"${API_HOST}"':5000/uploads; \
        proxy_http_version 1.1; \
        proxy_set_header Host $host; \
        proxy_cache_valid 200 1d; \
        add_header Cache-Control "public, max-age=86400"; \
    } \
    \
    # Admin Panel - SPA Routing \
    location /admin { \
        try_files $uri $uri/ /index.html; \
    } \
    \
    # Ana sayfa ve diğer rotalar - SPA Fallback \
    location / { \
        try_files $uri $uri/ /index.html; \
    } \
    \
    # Static assets caching - sadece /static/ altındaki dosyalar için \
    location ~* ^/static/.*\.(js|css|png|jpg|jpeg|gif|ico|svg|woff|woff2|ttf|eot)$ { \
        expires 1y; \
        add_header Cache-Control "public, immutable"; \
    } \
}' > /etc/nginx/conf.d/default.conf

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]

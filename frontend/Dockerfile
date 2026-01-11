# Build locally first, then copy
FROM nginx:stable-alpine
WORKDIR /usr/share/nginx/html

# Copy pre-built files from local build directory
COPY build .

# API backend hostname - docker-compose'dan geçirilir veya default kullanılır
ARG API_HOST=ecommerce-api
ENV API_HOST=${API_HOST}

# Nginx config: React SPA routing + API proxy
# Frontend'den /api istekleri Docker network içindeki API container'a yönlendirilir
RUN echo 'server { \
    listen 80; \
    root /usr/share/nginx/html; \
    index index.html; \
    \
    # API Proxy - Docker network içinden backend'e yönlendir \
    location /api { \
        proxy_pass http://'"${API_HOST}"':5000; \
        proxy_http_version 1.1; \
        proxy_set_header Upgrade $http_upgrade; \
        proxy_set_header Connection "upgrade"; \
        proxy_set_header Host $host; \
        proxy_set_header X-Real-IP $remote_addr; \
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for; \
        proxy_set_header X-Forwarded-Proto $scheme; \
        proxy_cache_bypass $http_upgrade; \
        proxy_read_timeout 300s; \
        proxy_connect_timeout 75s; \
    } \
    \
    # React Router - SPA fallback \
    location / { \
        try_files $uri /index.html; \
    } \
    \
    # Static assets caching \
    location ~* \.(js|css|png|jpg|jpeg|gif|ico|svg|woff|woff2|ttf|eot)$ { \
        expires 1y; \
        add_header Cache-Control "public, immutable"; \
    } \
}' > /etc/nginx/conf.d/default.conf

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]

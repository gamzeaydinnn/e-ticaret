FROM node:18-alpine AS build
WORKDIR /app

# Copy package files first for better caching
COPY package.json package-lock.json* ./

# Install dependencies
RUN npm ci --production=false --legacy-peer-deps --no-fund --no-audit

# Copy source and build
COPY . .
RUN npm run build

# Production stage
FROM nginx:stable-alpine
WORKDIR /usr/share/nginx/html
COPY --from=build /app/build .

# Copy nginx config for React Router support and API proxy
RUN echo 'upstream api { \
    server ecommerce-api-prod:5000; \
} \
upstream mock_api { \
    server ecommerce-mock-api-prod:3005; \
} \
server { \
    listen 80; \
    root /usr/share/nginx/html; \
    index index.html; \
    location /api { \
        proxy_pass http://api; \
        proxy_http_version 1.1; \
        proxy_set_header Upgrade $http_upgrade; \
        proxy_set_header Connection '"'"'upgrade'"'"'; \
        proxy_set_header Host $host; \
        proxy_set_header X-Real-IP $remote_addr; \
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for; \
        proxy_set_header X-Forwarded-Proto $scheme; \
        proxy_cache_bypass $http_upgrade; \
    } \
    location /products { \
        proxy_pass http://mock_api/products; \
        proxy_http_version 1.1; \
        proxy_set_header Host $host; \
        proxy_set_header X-Real-IP $remote_addr; \
    } \
    location /posters { \
        proxy_pass http://mock_api/posters; \
        proxy_http_version 1.1; \
        proxy_set_header Host $host; \
        proxy_set_header X-Real-IP $remote_addr; \
    } \
    location /coupons { \
        proxy_pass http://mock_api/coupons; \
        proxy_http_version 1.1; \
        proxy_set_header Host $host; \
        proxy_set_header X-Real-IP $remote_addr; \
    } \
    location /reviews { \
        proxy_pass http://mock_api/reviews; \
        proxy_http_version 1.1; \
        proxy_set_header Host $host; \
        proxy_set_header X-Real-IP $remote_addr; \
    } \
    location / { \
        try_files $uri /index.html; \
    } \
    location ~* \.(js|css|png|jpg|jpeg|gif|ico|svg|woff|woff2|ttf|eot)$ { \
        expires 1y; \
        add_header Cache-Control "public, immutable"; \
    } \
}' > /etc/nginx/conf.d/default.conf

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]

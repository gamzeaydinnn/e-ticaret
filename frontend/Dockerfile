FROM node:18-alpine AS build
WORKDIR /app

# Build arguments from docker-compose
ARG REACT_APP_API_URL=https://golkoygurme.com.tr/api
ARG REACT_APP_BACKEND_ENABLED=true
ARG REACT_APP_USE_MOCK_DATA=false

# Set as ENV so they persist in this RUN block
ENV REACT_APP_API_URL=${REACT_APP_API_URL}
ENV REACT_APP_BACKEND_ENABLED=${REACT_APP_BACKEND_ENABLED}
ENV REACT_APP_USE_MOCK_DATA=${REACT_APP_USE_MOCK_DATA}
ENV NODE_ENV=production

# Copy package files first
COPY package.json package-lock.json* ./

# Install dependencies
RUN npm ci --production=false --legacy-peer-deps --no-fund --no-audit

# Copy source code
COPY . .

# Build with ENV variables that are now set
RUN echo "=== Build environment ===" && \
    echo "REACT_APP_API_URL=${REACT_APP_API_URL}" && \
    echo "REACT_APP_BACKEND_ENABLED=${REACT_APP_BACKEND_ENABLED}" && \
    echo "REACT_APP_USE_MOCK_DATA=${REACT_APP_USE_MOCK_DATA}" && \
    echo "=== Starting npm build ===" && \
    npm run build && \
    echo "=== Build complete, checking for API URL in output ===" && \
    grep -r "golkoygurme" build/static/js/*.js | head -1 || echo "WARNING: API URL not found in build output"

# Production stage
FROM nginx:stable-alpine
WORKDIR /usr/share/nginx/html
COPY --from=build /app/build .

# Copy nginx config for React Router support and API proxy
RUN echo 'upstream api { \
    server ecommerce-api-prod:5000; \
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

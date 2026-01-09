FROM node:18-alpine AS build
WORKDIR /app

# Build arguments from docker-compose
ARG REACT_APP_API_URL=https://golkoygurme.com.tr/api
ARG REACT_APP_BACKEND_ENABLED=true
ARG REACT_APP_USE_MOCK_DATA=false

# Create .env file FIRST - BEFORE copying source or npm install
RUN echo "REACT_APP_API_URL=${REACT_APP_API_URL}" > .env && \
    echo "REACT_APP_BACKEND_ENABLED=${REACT_APP_BACKEND_ENABLED}" >> .env && \
    echo "REACT_APP_USE_MOCK_DATA=${REACT_APP_USE_MOCK_DATA}" >> .env && \
    echo "=== .env file created ===" && \
    cat .env

# Copy package files
COPY package.json package-lock.json* ./

# Install dependencies (will use .env created above)
RUN npm ci --production=false --legacy-peer-deps --no-fund --no-audit

# Copy source code
COPY . .

# Verify .env is still correct (in case source had .env file)
RUN echo "=== Verifying .env before build ===" && \
    cat .env

# Build with NODE_ENV=production to ensure production optimizations
RUN NODE_ENV=production npm run build

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

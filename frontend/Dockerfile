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

# Copy nginx config for React Router support
RUN echo 'server { \
    listen 80; \
    root /usr/share/nginx/html; \
    index index.html; \
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

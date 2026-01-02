FROM node:18-alpine AS build
WORKDIR /app

# Install dependencies with better caching and retry logic
COPY package.json ./
RUN npm config set registry https://registry.npmjs.org/ \
 && npm config set fetch-retries 10 \
 && npm config set fetch-retry-factor 3 \
 && npm config set fetch-retry-maxtimeout 120000 \
 && npm config set fetch-retry-mintimeout 5000 \
 && npm install --no-audit --no-fund --legacy-peer-deps --prefer-offline --no-optional 2>&1 | tail -100 || npm install --no-audit --no-fund --legacy-peer-deps

# Copy sources and build
COPY . .
RUN npx react-scripts build --max_old_space_size=2048

FROM nginx:stable-alpine
COPY --from=build /app/build /usr/share/nginx/html
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]

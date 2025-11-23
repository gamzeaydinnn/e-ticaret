FROM node:18-alpine AS build
WORKDIR /app

# Install dependencies (use npm install when lockfile may be absent)
COPY package.json ./
# Ensure registry and retry settings to be more tolerant on flaky/mobile networks
RUN npm config set registry https://registry.npmjs.org/ \
 && npm config set fetch-retries 5 \
 && npm config set fetch-retry-factor 2 \
 && npm config set fetch-retry-maxtimeout 60000 \
 && npm config set fetch-retry-mintimeout 2000 \
 && npm install --no-audit --no-fund --verbose --legacy-peer-deps

# Copy sources and build
COPY . .
RUN npx react-scripts build

FROM nginx:stable-alpine
COPY --from=build /app/build /usr/share/nginx/html
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]

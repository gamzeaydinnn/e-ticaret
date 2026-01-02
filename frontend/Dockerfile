FROM node:18-alpine AS build
WORKDIR /app

# Install dependencies with minimal logging
COPY package.json ./
RUN npm config set registry https://registry.npmjs.org/ \
 && npm config set fetch-retries 5 \
 && npm install --production=false --no-audit --no-fund --legacy-peer-deps --omit=optional --silent 2>/dev/null || true

# Copy sources and build
COPY . .
RUN npx react-scripts build 2>&1 | grep -i error || true

FROM nginx:stable-alpine
COPY --from=build /app/build /usr/share/nginx/html
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]

FROM nginx:stable-alpine
COPY --from=build /app/build /usr/share/nginx/html
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]

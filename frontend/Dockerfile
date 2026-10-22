
# Stage 1: Build Angular
FROM node:22-alpine AS build

WORKDIR /app

COPY package*.json ./

RUN npm ci

COPY . .

RUN npm run build -- --configuration production

# Stage 2: Serve Angular with Nginx
FROM nginx:stable-alpine AS runtime

COPY nginx.conf /etc/nginx/conf.d/default.conf

COPY --from=build /app/dist/frontend/browser/ /usr/share/nginx/html/

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]


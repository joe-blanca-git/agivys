# Estágio 1: Build
FROM node:20-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm install
COPY . .
# --base-href precisa bater com o caminho no nginx (/agivys/portal/)
RUN npm run build -- --base-href /agivys/portal/

# Estágio 2: Nginx servindo os arquivos estáticos
# (este app não tem SSR configurado — é um SPA, igual a landing)
FROM nginx:alpine
COPY --from=build /app/dist/bit-admin/browser /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf

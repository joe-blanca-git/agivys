# Estágio 1: Build
FROM node:20-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm install
COPY . .
RUN npm run build

# Estágio 2: Servidor Node (SSR)
FROM node:20-alpine
WORKDIR /app

# Copia os arquivos compilados (tanto o browser quanto o server)
COPY --from=build /app/dist/bit-admin ./dist/bit-admin
COPY package.json ./

# Força o Node a rodar na porta 80 para não precisarmos mexer no docker-compose
ENV PORT=80
EXPOSE 80

# Inicia o servidor Node configurado no package.json
CMD ["npm", "run", "serve:ssr:bit-admin"]

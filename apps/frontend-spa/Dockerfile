FROM node:20-alpine AS build
WORKDIR /workspace

COPY apps/frontend-spa/package.json apps/frontend-spa/package-lock.json ./apps/frontend-spa/
RUN npm ci --prefix ./apps/frontend-spa

COPY . .

ARG VITE_AUTH0_DOMAIN=""
ARG VITE_AUTH0_AUDIENCE=""
ARG VITE_AUTH0_CLIENT_ID=""
ARG VITE_MARKET_API_BASE_URL="http://localhost:7001"
ARG VITE_TRADE_API_BASE_URL="http://localhost:7002"
ARG VITE_SETTLEMENT_API_BASE_URL="http://localhost:7003"
ARG VITE_MARKET_HUB_URL="http://localhost:7001/hubs/market"
ARG VITE_ENABLE_DEMO_AUTH="true"

ENV VITE_AUTH0_DOMAIN=${VITE_AUTH0_DOMAIN}
ENV VITE_AUTH0_AUDIENCE=${VITE_AUTH0_AUDIENCE}
ENV VITE_AUTH0_CLIENT_ID=${VITE_AUTH0_CLIENT_ID}
ENV VITE_MARKET_API_BASE_URL=${VITE_MARKET_API_BASE_URL}
ENV VITE_TRADE_API_BASE_URL=${VITE_TRADE_API_BASE_URL}
ENV VITE_SETTLEMENT_API_BASE_URL=${VITE_SETTLEMENT_API_BASE_URL}
ENV VITE_MARKET_HUB_URL=${VITE_MARKET_HUB_URL}
ENV VITE_ENABLE_DEMO_AUTH=${VITE_ENABLE_DEMO_AUTH}

RUN npm --prefix ./apps/frontend-spa run build

FROM nginx:1.27-alpine AS final
COPY apps/frontend-spa/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /workspace/apps/frontend-spa/dist /usr/share/nginx/html
COPY apps/frontend-spa/config.js.template /opt/runtime-config/config.js.template
COPY apps/frontend-spa/docker-entrypoint.d/40-write-runtime-config.sh /docker-entrypoint.d/40-write-runtime-config.sh
RUN chmod +x /docker-entrypoint.d/40-write-runtime-config.sh

EXPOSE 80

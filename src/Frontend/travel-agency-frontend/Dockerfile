FROM node:26-alpine AS build
ARG VITE_API_URL
ENV VITE_API_URL=$VITE_API_URL
WORKDIR /app
COPY package*.json ./
# Retry npm ci on transient network errors (ECONNRESET, ETIMEDOUT)
RUN npm config set fetch-retries 5 && \
    npm config set fetch-retry-mintimeout 20000 && \
    npm config set fetch-retry-maxtimeout 120000 && \
    (npm ci || npm ci || npm ci)
COPY . .
RUN npm run build

FROM nginx:alpine AS final
COPY --from=build /app/dist /usr/share/nginx/html
# Конфигурация nginx: HTTP по умолчанию, HTTPS и basic auth включаются entrypoint-скриптом,
# если смонтированы /etc/nginx/certs/tls.{crt,key} и /etc/nginx/auth/htpasswd
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY nginx.conf /etc/nginx/travelagency/http.conf
COPY nginx-https.conf /etc/nginx/travelagency/https.conf
COPY nginx-app.conf /etc/nginx/travelagency/app.conf
COPY nginx-headers.conf /etc/nginx/travelagency/headers.conf
COPY nginx-logging.conf /etc/nginx/conf.d/01-travelagency-logging.conf
COPY --chmod=755 nginx-entrypoint.sh /docker-entrypoint.d/40-travelagency.sh
# Значения по умолчанию, если entrypoint не запускался (basic auth выключен)
RUN touch /etc/nginx/travelagency/auth.conf && \
    printf 'map $remote_user $ta_gate_cookie { default ""; }\n' > /etc/nginx/conf.d/00-travelagency-gate.conf
EXPOSE 80 443
CMD ["nginx", "-g", "daemon off;"]

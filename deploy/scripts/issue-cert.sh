#!/usr/bin/env bash
# Выпуск (или повторный выпуск) сертификата Let's Encrypt для домена из SITE_DOMAIN.
# Запуск на сервере: sudo /opt/travelagency/deploy/scripts/issue-cert.sh
#
# Сменить домен: поправить SITE_DOMAIN в /opt/travelagency/docker/.env, добавить A-запись,
# запустить этот скрипт. Всё. Скрипт пересоздаёт frontend и gateway, чтобы они увидели новый домен.
#
# Повторный запуск безопасен: если сертификат действует, certbot его не трогает (--keep-until-expiring).
# Продление делает systemd-таймер certbot (certbot.timer), после продления хук перечитывает nginx.
set -euo pipefail

APP_DIR="${APP_DIR:-/opt/travelagency}"
ENV_FILE="$APP_DIR/docker/.env"
WEBROOT="${WEBROOT:-/var/lib/travelagency/acme}"
HOOK=/etc/letsencrypt/renewal-hooks/deploy/travelagency-nginx-reload.sh

[ "$(id -u)" -eq 0 ] || { echo "Нужен root: sudo $0" >&2; exit 1; }
command -v certbot >/dev/null || { echo "certbot не установлен: apt-get install -y certbot" >&2; exit 1; }

# Читаем только SITE_DOMAIN, остальной .env не печатаем и не загружаем
SITE_DOMAIN=$(sed -n 's/^SITE_DOMAIN=//p' "$ENV_FILE" | tail -n1 | tr -d "\"' \r")
if [ -z "$SITE_DOMAIN" ] || ! printf '%s' "$SITE_DOMAIN" | grep -Eq '^[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$'; then
    echo "SITE_DOMAIN в $ENV_FILE не задан или некорректен" >&2; exit 1
fi
EMAIL="${CERTBOT_EMAIL:-}"
echo "Домен: $SITE_DOMAIN"

# Домен должен уже указывать на этот сервер, иначе Let's Encrypt не достучится
if ! getent ahostsv4 "$SITE_DOMAIN" >/dev/null; then
    echo "DNS для $SITE_DOMAIN пока не резолвится, выпуск невозможен" >&2; exit 1
fi

mkdir -p "$WEBROOT"

# Хук: после каждого выпуска и продления nginx во frontend перечитывает сертификат без простоя
mkdir -p "$(dirname "$HOOK")"
cat > "$HOOK" <<HOOK_EOF
#!/bin/sh
# Создано issue-cert.sh: перечитать nginx после выпуска или продления сертификата
cd "$APP_DIR/docker" && docker compose exec -T frontend nginx -s reload
HOOK_EOF
chmod 755 "$HOOK"

# nginx должен работать с актуальным SITE_DOMAIN, чтобы отдать файл проверки HTTP-01
cd "$APP_DIR/docker"
docker compose up -d frontend gateway

if [ -n "$EMAIL" ]; then contact=(--email "$EMAIL"); else contact=(--register-unsafely-without-email); fi
certbot certonly --webroot -w "$WEBROOT" -d "$SITE_DOMAIN" \
    --non-interactive --agree-tos "${contact[@]}" --keep-until-expiring

# Первый выпуск: nginx стартовал с самоподписанным сертификатом, перезапуск подхватит Let's Encrypt
docker compose up -d --force-recreate frontend
systemctl enable --now certbot.timer >/dev/null 2>&1 || true
echo "Готово: https://$SITE_DOMAIN"

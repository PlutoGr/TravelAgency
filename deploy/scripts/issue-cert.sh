#!/usr/bin/env bash
# Выпуск (или повторный выпуск) сертификата Let's Encrypt для домена из SITE_DOMAIN.
# Запуск на сервере: sudo /opt/travelagency/deploy/scripts/issue-cert.sh
#
# Сменить домен: поправить SITE_DOMAIN в /opt/travelagency/docker/.env, добавить A-запись,
# запустить этот скрипт. Всё. Скрипт пересоздаёт frontend и gateway, чтобы они увидели новый домен.
#
# Повторный запуск безопасен: если сертификат действует, certbot его не трогает (--keep-until-expiring).
# Продление делает systemd-таймер certbot (certbot.timer), после продления хук перечитывает nginx.
#
# docker compose запускается не от root, а от пользователя deploy (как при деплое из CI) и только
# с уже скачанными образами той версии, что сейчас работает: --no-build --pull never. Без этого
# compose без IMAGE_TAG и логина в GHCR молча собирал бы все образы из исходников с тегом latest.
# Версию можно задать явно: sudo IMAGE_TAG=<sha> .../issue-cert.sh
set -euo pipefail

APP_DIR="${APP_DIR:-/opt/travelagency}"
ENV_FILE="$APP_DIR/docker/.env"
WEBROOT="${WEBROOT:-/var/lib/travelagency/acme}"
HOOK=/etc/letsencrypt/renewal-hooks/deploy/travelagency-nginx-reload.sh
DEPLOY_USER="${DEPLOY_USER:-deploy}"
FRONTEND_IMAGE=ghcr.io/plutogr/travelagency-frontend

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

# docker compose от имени deploy в каталоге docker (там .env и override), с HOME deploy (~/.docker)
DEPLOY_HOME=$(getent passwd "$DEPLOY_USER" | cut -d: -f6)
[ -n "$DEPLOY_HOME" ] || { echo "Пользователь $DEPLOY_USER не найден" >&2; exit 1; }
cd "$APP_DIR/docker"
compose() {
    runuser -u "$DEPLOY_USER" -- env HOME="$DEPLOY_HOME" IMAGE_TAG="$IMAGE_TAG" docker compose "$@"
}

# Версия образов: тег того frontend, что работает сейчас. Не смогли определить: стоп, ничего не трогаем.
if [ -z "${IMAGE_TAG:-}" ]; then
    cid=$(runuser -u "$DEPLOY_USER" -- env HOME="$DEPLOY_HOME" docker compose ps -q frontend 2>/dev/null | head -n1 || true)
    image=""
    [ -n "$cid" ] && image=$(docker inspect -f '{{.Config.Image}}' "$cid" 2>/dev/null || true)
    case "$image" in
        "$FRONTEND_IMAGE":*) IMAGE_TAG="${image##*:}" ;;
        *)
            echo "Не удалось определить версию образов по запущенному frontend (образ: ${image:-нет})." >&2
            echo "Запустите с явной версией: sudo IMAGE_TAG=<полный sha> $0" >&2
            exit 1 ;;
    esac
fi
if ! printf '%s' "$IMAGE_TAG" | grep -Eq '^[A-Za-z0-9_][A-Za-z0-9_.-]{0,127}$'; then
    echo "Некорректный IMAGE_TAG: $IMAGE_TAG" >&2; exit 1
fi
echo "Версия образов: $IMAGE_TAG"

# nginx должен работать с актуальным SITE_DOMAIN, чтобы отдать файл проверки HTTP-01.
# --no-build и --pull never: только локальные образы, никакой сборки; --no-deps: остальные сервисы не трогаем.
compose up -d --no-build --pull never --no-deps frontend gateway

if [ -n "$EMAIL" ]; then contact=(--email "$EMAIL"); else contact=(--register-unsafely-without-email); fi
certbot certonly --webroot -w "$WEBROOT" -d "$SITE_DOMAIN" \
    --non-interactive --agree-tos "${contact[@]}" --keep-until-expiring

# Первый выпуск: nginx стартовал с самоподписанным сертификатом, перезапуск подхватит Let's Encrypt
compose up -d --no-build --pull never --no-deps --force-recreate frontend
systemctl enable --now certbot.timer >/dev/null 2>&1 || true
echo "Готово: https://$SITE_DOMAIN"

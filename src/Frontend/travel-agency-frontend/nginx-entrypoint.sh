#!/bin/sh
# Запускается образом nginx перед стартом (/docker-entrypoint.d).
# Включает basic auth и HTTPS, если на сервере смонтированы нужные файлы.
# Перезапуск контейнера заново рендерит конфиг; после продления сертификата хватает nginx -s reload.
set -e

AUTH_CONF=/etc/nginx/travelagency/auth.conf
GATE_CONF=/etc/nginx/conf.d/00-travelagency-gate.conf

HTTPS=0
if [ -s /etc/nginx/certs/tls.crt ] && [ -s /etc/nginx/certs/tls.key ]; then
    HTTPS=1
fi

if [ -s /etc/nginx/auth/htpasswd ]; then
    # После успешного входа по basic auth nginx ставит cookie ta_dev_gate, и дальше пускает по ней.
    # Без cookie браузер спрашивал пароль много раз: ответы 401 от API (Bearer, «не залогинен»)
    # Chrome считает отказом в basic auth и забывает введённый пароль.
    # Значение cookie: sha256 от htpasswd. Угадать его нельзя, а при смене пароля старые cookie перестают работать.
    GATE=$(sha256sum /etc/nginx/auth/htpasswd | cut -d' ' -f1)
    SECURE=""
    [ "$HTTPS" = 1 ] && SECURE="; Secure"
    cat > "$GATE_CONF" <<CONF
# Сгенерировано nginx-entrypoint.sh
# Ключ map длиной 64 символа не помещается в корзину по умолчанию
map_hash_bucket_size 128;
map \$cookie_ta_dev_gate \$ta_auth_realm {
    "$GATE" off;
    default  "TravelAgency dev";
}
map \$remote_user \$ta_gate_cookie {
    ""      "";
    default "ta_dev_gate=$GATE; Path=/; Max-Age=2592000; HttpOnly; SameSite=Lax$SECURE";
}
CONF
    printf 'auth_basic $ta_auth_realm;\nauth_basic_user_file /etc/nginx/auth/htpasswd;\n' > "$AUTH_CONF"
    echo "travelagency: basic auth включён"
else
    printf 'map $remote_user $ta_gate_cookie { default ""; }\n' > "$GATE_CONF"
    : > "$AUTH_CONF"
fi

if [ "$HTTPS" = 1 ]; then
    # Домен берётся из переменной SITE_DOMAIN (на сервере: docker/.env, в compose передаётся во frontend).
    if [ -z "${SITE_DOMAIN:-}" ]; then
        echo "travelagency: смонтирован сертификат, но SITE_DOMAIN не задан" >&2
        exit 1
    fi
    case "$SITE_DOMAIN" in
        *[!a-zA-Z0-9.-]*) echo "travelagency: недопустимый SITE_DOMAIN" >&2; exit 1 ;;
    esac
    # Плавная деградация: пока сертификата Let's Encrypt нет (домен ещё не делегирован или не выпущен),
    # домен обслуживается самоподписанным, а заход по IP работает как раньше, без редиректа на домен.
    # Когда сертификат появился (issue-cert.sh пересоздаёт контейнер), заход по IP уводится на домен.
    LE_DIR="/etc/letsencrypt/live/$SITE_DOMAIN"
    if [ -s "$LE_DIR/fullchain.pem" ] && [ -s "$LE_DIR/privkey.pem" ]; then
        TA_CERT="$LE_DIR/fullchain.pem"; TA_KEY="$LE_DIR/privkey.pem"; MODE=redirect
    else
        TA_CERT=/etc/nginx/certs/tls.crt; TA_KEY=/etc/nginx/certs/tls.key; MODE=fallback
        echo "travelagency: сертификата Let's Encrypt для $SITE_DOMAIN нет, по IP сайт работает как раньше"
    fi
    export SITE_DOMAIN TA_CERT TA_KEY
    T=/etc/nginx/travelagency
    # shellcheck disable=SC2016 # список переменных для envsubst, раскрывать его не нужно
    VARS='${SITE_DOMAIN} ${TA_CERT} ${TA_KEY}'
    envsubst "$VARS" < "$T/domain.conf.template" > /etc/nginx/conf.d/default.conf
    envsubst "$VARS" < "$T/default/$MODE-http.conf" > "$T/default-http.conf"
    envsubst "$VARS" < "$T/default/$MODE-https.conf" > "$T/default-https.conf"
    echo "travelagency: заход по IP в режиме $MODE"
    echo "travelagency: HTTPS включён для $SITE_DOMAIN"
else
    cp /etc/nginx/travelagency/http.conf /etc/nginx/conf.d/default.conf
fi

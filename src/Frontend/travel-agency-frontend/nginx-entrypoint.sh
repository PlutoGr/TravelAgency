#!/bin/sh
# Запускается образом nginx перед стартом (/docker-entrypoint.d).
# Включает basic auth и HTTPS, если на сервере смонтированы нужные файлы.
set -e

AUTH_CONF=/etc/nginx/travelagency/auth.conf
if [ -s /etc/nginx/auth/htpasswd ]; then
    printf 'auth_basic "TravelAgency dev";\nauth_basic_user_file /etc/nginx/auth/htpasswd;\n' > "$AUTH_CONF"
    echo "travelagency: basic auth включён"
else
    : > "$AUTH_CONF"
fi

if [ -s /etc/nginx/certs/tls.crt ] && [ -s /etc/nginx/certs/tls.key ]; then
    cp /etc/nginx/travelagency/https.conf /etc/nginx/conf.d/default.conf
    echo "travelagency: HTTPS включён"
else
    cp /etc/nginx/travelagency/http.conf /etc/nginx/conf.d/default.conf
fi

#!/bin/sh
set -eu

template_path="/opt/runtime-config/config.js.template"
output_path="/usr/share/nginx/html/config.js"

envsubst '
${VITE_AUTH0_DOMAIN}
${VITE_AUTH0_AUDIENCE}
${VITE_AUTH0_CLIENT_ID}
${VITE_MARKET_API_BASE_URL}
${VITE_TRADE_API_BASE_URL}
${VITE_SETTLEMENT_API_BASE_URL}
${VITE_MARKET_HUB_URL}
${VITE_ENABLE_DEMO_AUTH}
' < "$template_path" > "$output_path"

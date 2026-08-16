#!/usr/bin/env bash
#
# Kiểm tra hợp đồng định tuyến của reverse proxy sau khi deploy.
#
#   ./scripts/smoke-test-routing.sh [BASE_URL]
#   ./scripts/smoke-test-routing.sh https://tedf.io.vn
#
# Vì SPA build với VITE_API_BASE_URL="" (same-origin), mọi path backend phải được
# nginx chuyển tới container API. Path nào thiếu location sẽ rơi xuống SPA fallback
# và nhận về "HTTP 200 + text/html" — client parse HTML thành JSON rồi chết với
# "Unexpected token '<'". Kiểu hỏng này KHÔNG làm container unhealthy và KHÔNG hiện
# trong log, nên healthcheck của docker compose không bắt được.
#
# Assert quan trọng nhất ở đây không phải status code mà là: path backend TUYỆT ĐỐI
# không được trả về text/html.

set -Eeuo pipefail

BASE_URL="${1:-${SMOKE_BASE_URL:-https://tedf.io.vn}}"
BASE_URL="${BASE_URL%/}"

TIMEOUT="${SMOKE_TIMEOUT:-20}"

failures=0
checks=0

# check <mô tả> <method> <path> <status mong đợi> <html_allowed: yes|no>
check() {
    local desc="$1" method="$2" path="$3" want_status="$4" html_ok="$5"
    local out status ctype

    checks=$((checks + 1))

    out=$(curl -sS -X "$method" \
               -o /dev/null \
               -w '%{http_code} %{content_type}' \
               --max-time "$TIMEOUT" \
               "${BASE_URL}${path}" 2>/dev/null) || {
        printf 'FAIL  %-46s không gọi được (curl lỗi)\n' "$desc"
        failures=$((failures + 1))
        return
    }

    status="${out%% *}"
    ctype="${out#* }"

    # Backend path trả về HTML = nginx định tuyến sai, luôn là lỗi.
    if [ "$html_ok" = "no" ] && [[ "$ctype" == text/html* ]]; then
        printf 'FAIL  %-46s trả về text/html — nginx thiếu location cho path này\n' "$desc"
        printf '      %s %s → %s %s\n' "$method" "$path" "$status" "$ctype"
        failures=$((failures + 1))
        return
    fi

    if [ "$status" != "$want_status" ]; then
        printf 'FAIL  %-46s mong đợi %s, nhận %s (%s)\n' "$desc" "$want_status" "$status" "${ctype:-no content-type}"
        failures=$((failures + 1))
        return
    fi

    printf 'ok    %-46s %s\n' "$desc" "$status"
}

echo "Smoke test định tuyến: $BASE_URL"
echo

# --- Backend: KHÔNG được trả text/html ---------------------------------------
check "health check"                GET  "/health"                                  200 no
check "REST API cần auth"           GET  "/api/notifications"                       401 no
check "REST API public settings"    GET  "/api/settings"                            401 no

# Guard chính cho bug SignalR. 401 = backend đã nhận request và từ chối vì thiếu
# token — đó là kết quả ĐÚNG. 200 + text/html = SPA đang trả lời thay backend.
check "SignalR negotiate (notif)"   POST "/hubs/notifications/negotiate?negotiateVersion=1" 401 no
check "SignalR negotiate (chat)"    POST "/hubs/chat/negotiate?negotiateVersion=1"          401 no

# --- Frontend: PHẢI trả text/html --------------------------------------------
check "SPA index"                   GET  "/"                                        200 yes
check "SPA client-side route"       GET  "/login"                                   200 yes

echo
if [ "$failures" -gt 0 ]; then
    echo "FAILED: $failures/$checks check không đạt."
    exit 1
fi

echo "PASSED: $checks/$checks check đạt."

#!/usr/bin/env bash
#
# block-sensitive-files.sh
#
# pre-commit hook (commit stage): refuse to commit files whose names match
# common secret / credential patterns. This is a safety net layered on top of
# .gitignore (which can be bypassed with `git add -f`).
#
# pre-commit passes the staged file paths as positional arguments.
#
set -euo pipefail

# --- Filenames that are always allowed (templates / examples) ---------------
allow_regex='(\.env\.(example|sample|template)$|\.env\..*\.example$|\.env\.example$)'

# --- Sensitive filename patterns (matched case-insensitively) ---------------
patterns=(
  '(^|/)\.env(\.|$)'                       # .env, .env.local, .env.production ...
  '(^|/)\.npmrc$'                          # may hold registry auth tokens
  '\.(pem|key|pfx|p12|pkcs12|keystore|jks|p8)$'
  '(^|/)id_(rsa|dsa|ecdsa|ed25519)(\.|$)'  # SSH / private keys
  '(^|/)[^/]*secret[^/]*\.(json|ya?ml)$'   # *secret*.json | *secret*.yml | yaml
  '(^|/)credentials?\.(json|ya?ml)$'
  '(^|/)appsettings(\..*)?\.secrets\.json$'
  '(^|/)[^/]*serviceaccount[^/]*\.json$'   # Firebase / GCP service-account keys
  '(^|/)firebase-adminsdk[^/]*\.json$'
)

blocked=()
for file in "$@"; do
  # Allow explicit template / example files.
  if printf '%s' "$file" | grep -Eiq "$allow_regex"; then
    continue
  fi
  for pat in "${patterns[@]}"; do
    if printf '%s' "$file" | grep -Eiq "$pat"; then
      blocked+=("$file")
      break
    fi
  done
done

if [[ "${#blocked[@]}" -gt 0 ]]; then
  echo "✖ Blocked: the following staged file(s) look sensitive and must not be committed:" >&2
  for f in "${blocked[@]}"; do
    echo "    - $f" >&2
  done
  echo >&2
  echo "  If this is a genuine template, name it *.example / *.sample / *.template." >&2
  echo "  To override intentionally (discouraged): git commit --no-verify" >&2
  exit 1
fi

exit 0

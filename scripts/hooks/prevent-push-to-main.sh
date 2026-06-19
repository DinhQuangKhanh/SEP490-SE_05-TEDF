#!/usr/bin/env bash
#
# prevent-push-to-main.sh
#
# pre-commit hook (pre-push stage): block pushing to a protected branch.
# At the pre-push stage pre-commit exports:
#   PRE_COMMIT_REMOTE_BRANCH  destination ref, e.g. refs/heads/main
#   PRE_COMMIT_LOCAL_BRANCH   source ref
#
set -euo pipefail

protected='^(refs/heads/)?(main|master)$'

target="${PRE_COMMIT_REMOTE_BRANCH:-}"

# Fallback when run outside pre-commit's pre-push stage: use the checked-out branch.
if [[ -z "$target" ]]; then
  target="$(git rev-parse --abbrev-ref HEAD 2>/dev/null || echo '')"
fi

if printf '%s' "$target" | grep -Eq "$protected"; then
  branch="${target#refs/heads/}"
  echo "✖ Direct pushes to '$branch' are blocked." >&2
  echo "  Push a feature branch and open a pull request instead." >&2
  echo "  To override intentionally (discouraged): git push --no-verify" >&2
  exit 1
fi

exit 0

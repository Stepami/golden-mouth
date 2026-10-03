#!/bin/sh
# Публичный репозиторий: блокируем коммит, если в изменениях похоже на секрет.
# Запуск вручную: .github/scripts/check-secrets.sh --all (весь репозиторий) или без флага (только staged).
set -e
if [ "$1" = "--all" ]; then FILES=$(git ls-files); else FILES=$(git diff --cached --name-only --diff-filter=ACM); fi
[ -z "$FILES" ] && exit 0
BAD=0
for f in $FILES; do
  case "$f" in *.png|*.jpg|*.svg|*.ico|*.woff*|package-lock.json) continue;; esac
  case "$(basename "$f")" in .env|.env.*) [ "$(basename "$f")" = ".env.example" ] || { echo "✗ $f: файлы .env не коммитим"; BAD=1; continue; };; esac
  [ -f "$f" ] || continue
  if grep -nEI \
    -e 'ghp_[A-Za-z0-9]{30,}|github_pat_[A-Za-z0-9_]{30,}|gho_[A-Za-z0-9]{30,}' \
    -e 'sk-[A-Za-z0-9_-]{20,}|sk-ant-[A-Za-z0-9_-]{20,}' \
    -e 'AKIA[0-9A-Z]{16}|AIza[0-9A-Za-z_-]{35}|xox[abpr]-[A-Za-z0-9-]{10,}' \
    -e '[0-9]{8,10}:AA[A-Za-z0-9_-]{30,}' \
    -e '-----BEGIN [A-Z ]*PRIVATE KEY-----' \
    -e '(api[_-]?key|secret|token|passw(or)?d)["'"'"']?[[:space:]]*[:=][[:space:]]*["'"'"'][^"'"'"'$<{ ]{12,}' \
    -e 'mongodb(\+srv)?://[^/[:space:]]*:[^@[:space:]]+@' \
    -e '/Users/[a-z][a-z0-9_-]+/' \
    "$f" 2>/dev/null; then echo "✗ $f: похоже на секрет или личный путь"; BAD=1; fi
done
[ $BAD = 0 ] || { echo "Коммит остановлен. Секреты только в GitHub Secrets / переменных окружения сервера."; exit 1; }

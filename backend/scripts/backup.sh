#!/bin/sh
set -eu
umask 077
backend_dir=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
mkdir -p "$backend_dir/backups"
backup_file="$backend_dir/backups/tfbc-$(date -u +%Y%m%dT%H%M%SZ).dump"
docker compose --project-directory "$backend_dir" -f "$backend_dir/compose.yaml" exec -T db pg_dump -U tfbc -d tfbc -Fc > "$backup_file.tmp"
mv "$backup_file.tmp" "$backup_file"
printf 'Database backup saved: %s\n' "$backup_file"

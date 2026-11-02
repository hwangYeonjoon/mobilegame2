#!/usr/bin/env sh
set -eu
# Run from this directory. Produces a consistent SQLite snapshot (including WAL).
backup_name="scrapguard-$(date -u +%Y%m%dT%H%M%SZ)-$$.db"
mkdir -p backups
chmod 700 backups
docker compose exec -T api dotnet ScrapGuard.Server.dll --backup-database "/backups/$backup_name"
docker compose cp "api:/backups/$backup_name" "backups/$backup_name"
chmod 600 "backups/$backup_name"
printf 'Verified backup exported to %s\n' "backups/$backup_name"
# Copy this file to a separate, encrypted off-host backup destination.
# Local Docker volumes and local backup files do not survive loss of the VM.

#!/usr/bin/env sh
set -eu
# Run from this directory. Produces a consistent SQLite snapshot (including WAL).
backup_name="scrapguard-$(date -u +%Y%m%dT%H%M%SZ)-$$.db"
mkdir -p backups
chmod 700 backups
docker compose exec -T api dotnet ScrapGuard.Server.dll --backup-database "/backups/$backup_name"
docker compose cp "api:/backups/$backup_name" "backups/$backup_name"
chmod 600 "backups/$backup_name"
# The key ring is part of the backup; without it stored receipts cannot be rechecked.
if docker compose exec -T api test -d "/backups/$backup_name.keys"; then
  docker compose cp "api:/backups/$backup_name.keys" "backups/$backup_name.keys"
  chmod -R go-rwx "backups/$backup_name.keys"
fi
printf 'Verified backup exported to %s\n' "backups/$backup_name"
# Copy the .db file AND its .db.keys directory to an encrypted off-host destination.
# Local Docker volumes and local backup files do not survive loss of the VM.

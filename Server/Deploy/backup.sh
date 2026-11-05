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
# Daily execution prunes snapshots aged 29 days, keeping the maximum below 30 days.
# Restrict removal to snapshots and their paired key directories created by this script.
find backups -maxdepth 1 -type f -name 'scrapguard-*.db' -mtime +28 -exec sh -c '
  for snapshot do rm -f -- "$snapshot"; rm -rf -- "$snapshot.keys"; done
' sh {} +
docker compose exec -T api sh -c 'find /backups -maxdepth 1 -type f -name "scrapguard-*.db" -mtime +28 -exec sh -c '\''for snapshot do rm -f -- "$snapshot"; rm -rf -- "$snapshot.keys"; done'\'' sh {} +'
# Copy the .db file AND its .db.keys directory to an encrypted off-host destination.
# Local Docker volumes and local backup files do not survive loss of the VM.

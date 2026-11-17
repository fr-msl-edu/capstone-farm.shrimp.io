#!/usr/bin/env bash
set -euo pipefail
. "$(dirname "$0")/lib.sh"
DEST_ROOT="${1:-$BACKUP_ROOT}"
STAMP="$(date +%Y%m%d-%H%M%S)"
DEST="$DEST_ROOT/logs"
job_guard $KIND_LOG
mkdir -p "$DEST"

if [ "$MODE" = docker ]; then
  docker_backup_dir
  sqlcmd -Q "BACKUP LOG [$DB] TO DISK = N'/var/opt/mssql/backup/$DB-$STAMP.trn' WITH INIT, CHECKSUM, COMPRESSION" >/dev/null
  docker compose -f "$COMPOSE" cp "sqlserver:/var/opt/mssql/backup/$DB-$STAMP.trn" "$DEST/$DB-$STAMP.trn"
  docker compose -f "$COMPOSE" exec -T -u root sqlserver rm -f "/var/opt/mssql/backup/$DB-$STAMP.trn"
else
  sqlcmd -Q "BACKUP LOG [$DB] TO DISK = N'$DEST/$DB-$STAMP.trn' WITH INIT, CHECKSUM, COMPRESSION" >/dev/null
fi
SIZE="$(file_size "$DEST/$DB-$STAMP.trn")"
trap - ERR
record_run $KIND_LOG "$JOB_STARTED" 1 "logs/$DB-$STAMP.trn" "$SIZE" ""
echo "log backup: $DEST/$DB-$STAMP.trn ($SIZE bytes)"

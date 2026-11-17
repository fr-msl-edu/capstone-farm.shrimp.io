#!/usr/bin/env bash
set -euo pipefail

SCHEMASPY_IMAGE="schemaspy/schemaspy:6.2.4"
JDBC_VERSION="12.8.1.jre11"
JDBC_JAR="mssql-jdbc-${JDBC_VERSION}.jar"
JDBC_SHA256="e6933c0711e598a224060e52ed31392f720a4a7664e85d8ae37c52a85b67ebb0"  # Maven Central's .sha256 for this jar
JDBC_URL="https://repo1.maven.org/maven2/com/microsoft/sqlserver/mssql-jdbc/${JDBC_VERSION}/${JDBC_JAR}"

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
COMPOSE="$ROOT/aspnet-core/docker/sqlserver/docker-compose.yml"
DB="FPMS_Simple"
TABLES='(dbo\.)?Fpms.*'   # matches the table name with or without its schema
OUT="$ROOT/docs/erd"

while [ $# -gt 0 ]; do
  case "$1" in
    --db) DB="${2:?--db needs a database name}"; shift 2 ;;
    --all) TABLES='.*'; shift ;;
    --out) OUT="${2:?--out needs a directory}"; shift 2 ;;
    -h|--help) sed -n '2,13p' "$0" | sed 's/^# \{0,1\}//'; exit 0 ;;
    *) echo "Unknown option: $1 (options: --db, --all, --out; run with --help for details)" >&2; exit 2 ;;
  esac
done
case "$DB" in *[!A-Za-z0-9_]*|'') echo "Database name may only use letters, digits and _: $DB" >&2; exit 2 ;; esac

fail() { echo "$*" >&2; exit 1; }

PW="${MSSQL_SA_PASSWORD:-}"
if [ -z "$PW" ] && [ -f "$ROOT/aspnet-core/docker/sqlserver/.env" ]; then
  PW="$(grep -E '^MSSQL_SA_PASSWORD=' "$ROOT/aspnet-core/docker/sqlserver/.env" | head -1 | cut -d= -f2- | tr -d '"')"
fi
if [ -z "$PW" ] && [ -f "$ROOT/.devcontainer/local.env" ]; then
  PW="$(grep -E '^SA_PASSWORD=' "$ROOT/.devcontainer/local.env" | head -1 | cut -d= -f2- | tr -d '"')"
fi
[ -n "$PW" ] || fail "No SQL Server password found. Run .devcontainer/start-fpms.sh first, or set MSSQL_SA_PASSWORD."

docker info >/dev/null 2>&1 || fail "Docker is not running."

CID="$(docker compose -f "$COMPOSE" ps -q sqlserver 2>/dev/null || true)"
[ -n "$CID" ] && [ "$(docker inspect -f '{{.State.Running}}' "$CID" 2>/dev/null)" = true ] \
  || fail "SQL Server is not running. Start it with: bash .devcontainer/start-fpms.sh"
NETWORK="$(docker inspect -f '{{range $name, $_ := .NetworkSettings.Networks}}{{$name}} {{end}}' "$CID" | awk '{print $1}')"
[ -n "$NETWORK" ] || fail "Could not find the Docker network of the SQL Server container."

count="$(docker compose -f "$COMPOSE" exec -T -e SQLCMDPASSWORD="$PW" sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -C -S localhost -U sa -b -h -1 -W -Q "SET NOCOUNT ON; IF DB_ID('$DB') IS NULL SELECT -1 ELSE SELECT COUNT(*) FROM [$DB].sys.tables WHERE name LIKE 'Fpms%'" 2>&1 | tr -d '\r' | tail -1)" \
  || fail "Could not query SQL Server: $count"
case "$count" in
  -1) fail "Database $DB does not exist. Run start-fpms.sh (SEED_SAMPLE_DATA=0 creates FPMS, otherwise FPMS_Simple)." ;;
  0) fail "Database $DB has no Fpms tables yet. Let start-fpms.sh finish the migration first." ;;
  *[!0-9]*) fail "Unexpected answer from SQL Server: $count" ;;
esac
echo "Reading $DB ($count Fpms tables)..."

DRIVERS="${XDG_CACHE_HOME:-$HOME/.cache}/fpms-erd/drivers"
mkdir -p "$DRIVERS"
if [ ! -s "$DRIVERS/$JDBC_JAR" ]; then
  echo "Downloading the Microsoft SQL Server JDBC driver $JDBC_VERSION..."
  tmp="$DRIVERS/$JDBC_JAR.part"
  curl -fsSL "$JDBC_URL" -o "$tmp" || { rm -f "$tmp"; fail "Could not download $JDBC_URL"; }
  got="$(sha256sum "$tmp" | awk '{print $1}')"
  [ "$got" = "$JDBC_SHA256" ] || { rm -f "$tmp"; fail "Driver checksum does not match (expected $JDBC_SHA256, got $got)."; }
  mv "$tmp" "$DRIVERS/$JDBC_JAR"
fi
find "$DRIVERS" -maxdepth 1 -name '*.jar' ! -name "$JDBC_JAR" -delete

if [ -d "$OUT" ] && [ -n "$(ls -A "$OUT")" ]; then
  [ -f "$OUT/index.html" ] && [ -d "$OUT/tables" ] \
    || fail "$OUT is not empty and does not look like a SchemaSpy site. Empty it or choose another --out."
  find "$OUT" -mindepth 1 -delete
fi
mkdir -p "$OUT"
OUT="$(cd "$OUT" && pwd)"

CONF="$(mktemp -d)"
trap 'rm -rf "$CONF"' EXIT
(umask 077; printf 'schemaspy.p=%s\n' "$PW" > "$CONF/schemaspy.properties")

docker run --rm \
  --user "$(id -u):$(id -g)" -e HOME=/tmp \
  --network "$NETWORK" \
  -v "$DRIVERS:/drivers:ro" \
  -v "$CONF:/config:ro" \
  -v "$OUT:/output" \
  "$SCHEMASPY_IMAGE" \
  -configFile /config/schemaspy.properties \
  -t mssql17 -host sqlserver -port 1433 -db "$DB" -s dbo -u sa \
  -connprops 'encrypt\=true;trustServerCertificate\=true' \
  -i "$TABLES" \
  -norows

[ -f "$OUT/index.html" ] || fail "SchemaSpy finished without writing $OUT/index.html; see its output above."
echo
echo "Done: ${OUT#"$ROOT"/}/index.html"
echo "To browse it in the Codespace: npx --yes http-server ${OUT#"$ROOT"/} -p 8090 -c-1  (then open the forwarded port 8090)"

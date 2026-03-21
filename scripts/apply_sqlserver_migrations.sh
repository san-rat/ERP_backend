#!/usr/bin/env bash
set -euo pipefail

: "${AZURE_SQL_SERVER?Need AZURE_SQL_SERVER}"
: "${AZURE_SQL_DATABASE?Need AZURE_SQL_DATABASE}"
: "${AZURE_SQL_USER?Need AZURE_SQL_USER}"
: "${AZURE_SQL_PASSWORD?Need AZURE_SQL_PASSWORD}"

MIGRATIONS_DIR="${1:-schemas/auth/migrations}"
SCHEMA_NAME_OVERRIDE="${2:-${TARGET_SCHEMA_NAME:-}}"

if [ -n "$SCHEMA_NAME_OVERRIDE" ]; then
  SCHEMA_NAME="$SCHEMA_NAME_OVERRIDE"
else
  # The schema name is derived from the second path segment, e.g. "schemas/auth/migrations" -> "auth"
  SCHEMA_NAME=$(echo "$MIGRATIONS_DIR" | awk -F'/' '{print $2}')
fi

echo "==> Using migrations folder: $MIGRATIONS_DIR"
echo "==> Target: $AZURE_SQL_SERVER / $AZURE_SQL_DATABASE / $AZURE_SQL_USER"
echo "==> Schema: $SCHEMA_NAME"

# Add mssql-tools to path (GitHub Actions runner)
export PATH="$PATH:/opt/mssql-tools18/bin"

shopt -s nullglob
files=("$MIGRATIONS_DIR"/*.sql)

if [ ${#files[@]} -eq 0 ]; then
  echo "No .sql files found in $MIGRATIONS_DIR"
  exit 0
fi

ensure_schema_tracking() {
  # Create the schema if it doesn't exist
  sqlcmd -S "$AZURE_SQL_SERVER" -d "$AZURE_SQL_DATABASE" -U "$AZURE_SQL_USER" -P "$AZURE_SQL_PASSWORD" -C -Q "
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = '${SCHEMA_NAME}')
BEGIN
    EXEC('CREATE SCHEMA [${SCHEMA_NAME}]');
END
"

  # Create schema-specific migration tracking table if it doesn't exist
  sqlcmd -S "$AZURE_SQL_SERVER" -d "$AZURE_SQL_DATABASE" -U "$AZURE_SQL_USER" -P "$AZURE_SQL_PASSWORD" -C -Q "
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[${SCHEMA_NAME}].[schema_migrations]') AND type = N'U')
BEGIN
    CREATE TABLE [${SCHEMA_NAME}].schema_migrations (
        id INT IDENTITY(1,1) PRIMARY KEY,
        filename NVARCHAR(255) NOT NULL UNIQUE,
        applied_at DATETIME NOT NULL DEFAULT GETDATE()
    )
END
"
}

tracking_ready=0

for f in "${files[@]}"; do
  base="$(basename "$f")"

  if ! grep -q '[^[:space:]]' "$f"; then
    echo "WARN: $base is empty or whitespace-only; skipping without recording it"
    continue
  fi

  if [ "$tracking_ready" -eq 0 ]; then
    ensure_schema_tracking
    tracking_ready=1
  fi

  # Check if migration was already applied
  applied=$(sqlcmd -S "$AZURE_SQL_SERVER" -d "$AZURE_SQL_DATABASE" -U "$AZURE_SQL_USER" -P "$AZURE_SQL_PASSWORD" -C -h -1 -W -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM [${SCHEMA_NAME}].schema_migrations WHERE filename='$base';")
  applied=$(echo "$applied" | xargs)

  if [ "$applied" = "1" ]; then
    echo "SKIP: $base (already applied)"
    continue
  fi

  echo "APPLY: $base"
  sqlcmd -S "$AZURE_SQL_SERVER" -d "$AZURE_SQL_DATABASE" -U "$AZURE_SQL_USER" -P "$AZURE_SQL_PASSWORD" -C -i "$f" -b

  sqlcmd -S "$AZURE_SQL_SERVER" -d "$AZURE_SQL_DATABASE" -U "$AZURE_SQL_USER" -P "$AZURE_SQL_PASSWORD" -C -Q "SET NOCOUNT ON; INSERT INTO [${SCHEMA_NAME}].schema_migrations(filename) VALUES('$base');"

  echo "DONE: $base"
done

if [ "$tracking_ready" -eq 0 ]; then
  echo "No non-empty .sql files found in $MIGRATIONS_DIR"
  exit 0
fi

echo "All migrations applied for schema: $SCHEMA_NAME"

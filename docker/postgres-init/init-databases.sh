#!/bin/bash
set -e

# For each service database, create:
#   - a "<prefix>_migrator" role that owns the schema (DDL) - used only by the *.Migrator
#     console apps to apply EF Core migrations.
#   - a "<prefix>_app" role restricted to DML (SELECT/INSERT/UPDATE/DELETE) - used by the
#     running API at request time. It has no permission to CREATE/ALTER/DROP.
# Default privileges are set so that tables/sequences created later by the migrator role
# (i.e. every future migration) automatically grant DML to the app role, with no manual
# re-granting needed after each migration.
declare -A db_for_prefix=(
  [proposta]=proposta_db
  [contratacao]=contratacao_db
  [analise]=analise_db
)

for prefix in "${!db_for_prefix[@]}"; do
  db="${db_for_prefix[$prefix]}"
  migrator_user="${prefix}_migrator"
  app_user="${prefix}_app"
  prefix_upper=$(echo "$prefix" | tr '[:lower:]' '[:upper:]')
  migrator_password_var="${prefix_upper}_MIGRATOR_PASSWORD"
  app_password_var="${prefix_upper}_APP_PASSWORD"
  migrator_password="${!migrator_password_var}"
  app_password="${!app_password_var}"

  psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" <<-EOSQL
    SELECT 'CREATE DATABASE $db' WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = '$db')\gexec

    DO \$\$
    BEGIN
      IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = '$migrator_user') THEN
        CREATE ROLE $migrator_user LOGIN PASSWORD '$migrator_password';
      END IF;
      IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = '$app_user') THEN
        CREATE ROLE $app_user LOGIN PASSWORD '$app_password';
      END IF;
    END
    \$\$;
EOSQL

  psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$db" <<-EOSQL
    ALTER SCHEMA public OWNER TO $migrator_user;
    REVOKE CREATE ON SCHEMA public FROM PUBLIC;
    GRANT USAGE ON SCHEMA public TO $app_user;
    ALTER DEFAULT PRIVILEGES FOR ROLE $migrator_user IN SCHEMA public
      GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO $app_user;
    ALTER DEFAULT PRIVILEGES FOR ROLE $migrator_user IN SCHEMA public
      GRANT USAGE, SELECT ON SEQUENCES TO $app_user;
EOSQL
done

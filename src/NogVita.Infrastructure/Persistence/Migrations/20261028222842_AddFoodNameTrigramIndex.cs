using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NogVita.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFoodNameTrigramIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,")
                .Annotation("Npgsql:PostgresExtension:unaccent", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:unaccent", ",,");

            // D191: o Postgres só indexa funções IMMUTABLE, e o unaccent é STABLE (o dicionário pode mudar).
            // O f_unaccent é a mesma função, declarada imutável, e chama o unaccent pelo schema e pelo dicionário
            // explícitos, para não depender do search_path. O schema é descoberto na hora: no Postgres local fica no
            // "public"; no Supabase, as extensões costumam ficar no "extensions".
            migrationBuilder.Sql("""
                DO $migration$
                DECLARE
                    unaccent_schema text;
                    trgm_schema text;
                BEGIN
                    SELECT n.nspname INTO unaccent_schema
                    FROM pg_extension e JOIN pg_namespace n ON n.oid = e.extnamespace
                    WHERE e.extname = 'unaccent';

                    SELECT n.nspname INTO trgm_schema
                    FROM pg_extension e JOIN pg_namespace n ON n.oid = e.extnamespace
                    WHERE e.extname = 'pg_trgm';

                    EXECUTE format(
                        'CREATE OR REPLACE FUNCTION public.f_unaccent(text) RETURNS text
                         LANGUAGE sql IMMUTABLE PARALLEL SAFE STRICT
                         AS $f$ SELECT %I.unaccent(%L::regdictionary, $1) $f$',
                        unaccent_schema, unaccent_schema || '.unaccent');

                    -- Um índice de trigramas serve para o "contém" (ILIKE '%arroz%'), que um índice comum (btree) não cobre.
                    EXECUTE format(
                        'CREATE INDEX ix_food_name_trgm ON tb_food USING gin (public.f_unaccent(name) %I.gin_trgm_ops)',
                        trgm_schema);
                END
                $migration$;
                """);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_food_name_trgm;");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS public.f_unaccent(text);");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:unaccent", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:pg_trgm", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:unaccent", ",,");

        }
    }
}

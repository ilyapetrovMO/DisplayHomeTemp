﻿CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;


DO $$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20210106080423_Init') THEN
    CREATE TABLE "Temps" (
        "Id" integer NOT NULL GENERATED BY DEFAULT AS IDENTITY,
        "Temp" double precision NOT NULL,
        "Humidity" double precision NOT NULL,
        "Time" timestamp without time zone NOT NULL,
        CONSTRAINT "PK_Temps" PRIMARY KEY ("Id")
    );
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20210106080423_Init') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20210106080423_Init', '5.0.1');
    END IF;
END $$;
COMMIT;

START TRANSACTION;


DO $$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20210117135212_add-subscriptions-table') THEN
    CREATE TABLE "Subscriptions" (
        "Id" integer NOT NULL GENERATED BY DEFAULT AS IDENTITY,
        "ExpirationTime" integer NOT NULL,
        "Endpoint" text NULL,
        "P256DH" text NULL,
        "Auth" text NULL,
        CONSTRAINT "PK_Subscriptions" PRIMARY KEY ("Id")
    );
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20210117135212_add-subscriptions-table') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20210117135212_add-subscriptions-table', '5.0.1');
    END IF;
END $$;
COMMIT;


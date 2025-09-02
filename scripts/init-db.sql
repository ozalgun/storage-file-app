-- Storage File App Database Initialization Script
-- This script runs when the PostgreSQL container starts for the first time

-- Create additional databases if needed
-- CREATE DATABASE StorageFileApp_Test;
-- CREATE DATABASE StorageFileApp_Dev;

-- Create extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pg_trgm";

-- Create custom functions for better performance
CREATE OR REPLACE FUNCTION update_modified_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW."LastModifiedAt" = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Create indexes for better performance (will be created by EF Core migrations)
-- These are just examples, actual indexes will be created by migrations

-- Grant permissions
GRANT ALL PRIVILEGES ON DATABASE "StorageFileApp" TO storageuser;
GRANT ALL PRIVILEGES ON SCHEMA public TO storageuser;

-- Log successful initialization
DO $$
BEGIN
    RAISE NOTICE 'Storage File App database initialized successfully!';
END $$;

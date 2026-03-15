-- Create separate databases for each service (database-per-service pattern)
-- travel_identity is created by POSTGRES_DB; create the rest
CREATE DATABASE travel_catalog;
CREATE DATABASE travel_booking;
CREATE DATABASE travel_chat;
CREATE DATABASE travel_media;

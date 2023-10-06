CREATE USER producer WITH PASSWORD 'producer';
GRANT INSERT ON events TO producer;
GRANT SELECT ON events TO producer;
GRANT USAGE, SELECT ON SEQUENCE events_id_seq TO producer;

CREATE USER consumer WITH PASSWORD 'consumer';
GRANT SELECT ON events TO consumer;
GRANT CONNECT ON DATABASE eventsdb TO consumer;



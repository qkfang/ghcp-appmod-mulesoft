-- BookMyShow database initialisation
-- Run against the 'bookmyshow' database created by Bicep

CREATE TABLE IF NOT EXISTS movie_table (
    m_id        INT          NOT NULL AUTO_INCREMENT PRIMARY KEY,
    m_name      VARCHAR(255) NOT NULL,
    m_available INT          NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS order_table (
    o_id       INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    m_id       INT NOT NULL,
    no_tickets INT NOT NULL,
    price      INT NOT NULL
);

-- Seed data – a handful of movies with available seats
INSERT INTO movie_table (m_name, m_available) VALUES
    ('Avengers: Endgame', 50),
    ('The Lion King',     30),
    ('Spider-Man: No Way Home', 20),
    ('Doctor Strange',   40),
    ('Black Panther',    25);

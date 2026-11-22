-- =============================================================================
--  GameStation · Práctica 3 · Acceso a BBDD MySQL
--  Script de creación de la base de datos del catálogo de videojuegos.
--
--  Ejecutar como root (MySQL Workbench o consola):
--      mysql -u root -p < database.sql
--
--  Crea:
--    · La base de datos  catalogo_videojuegos
--    · La tabla          Usuarios (id, nombre_usuario, password)
--    · 3 usuarios de prueba
--    · Un usuario de MySQL para la aplicación (catalogo_app) con permisos
--      mínimos: solo SELECT e INSERT sobre la tabla Usuarios.
-- =============================================================================

CREATE DATABASE IF NOT EXISTS catalogo_videojuegos
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;

USE catalogo_videojuegos;

-- Se vuelve a crear la tabla para que el script se pueda ejecutar varias veces
DROP TABLE IF EXISTS Usuarios;

CREATE TABLE Usuarios (
    id              INT          NOT NULL AUTO_INCREMENT,
    nombre_usuario  VARCHAR(50)  NOT NULL,
    -- utf8mb4_bin: la contraseña distingue mayúsculas, minúsculas y acentos
    password        VARCHAR(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_bin NOT NULL,

    CONSTRAINT pk_usuarios PRIMARY KEY (id),
    -- El nombre de usuario no admite duplicados
    CONSTRAINT uq_usuarios_nombre_usuario UNIQUE (nombre_usuario)
);

-- Usuarios de prueba (los mismos que tenía la lista en memoria de la Práctica 2)
INSERT INTO Usuarios (nombre_usuario, password) VALUES
    ('admin',   '1234'),
    ('jugador', 'game2026'),
    ('alumno',  '12345');

-- -----------------------------------------------------------------------------
--  Usuario de conexión de la aplicación (el que usa DatabaseHelper)
-- -----------------------------------------------------------------------------
CREATE USER IF NOT EXISTS 'catalogo_app'@'localhost' IDENTIFIED BY 'Catalogo#2026';
ALTER USER 'catalogo_app'@'localhost' IDENTIFIED BY 'Catalogo#2026';

GRANT SELECT, INSERT ON catalogo_videojuegos.Usuarios TO 'catalogo_app'@'localhost';
FLUSH PRIVILEGES;

-- Comprobación
SELECT id, nombre_usuario, password FROM Usuarios;

DELIMITER //
CREATE PROCEDURE RenamePuntoVColumnsToULocale()
BEGIN
    DECLARE finished INT DEFAULT 0;
    DECLARE table_name_value VARCHAR(64);
    DECLARE column_cursor CURSOR FOR
        SELECT old_col.TABLE_NAME
        FROM INFORMATION_SCHEMA.COLUMNS AS old_col
        WHERE old_col.TABLE_SCHEMA = DATABASE()
          AND old_col.COLUMN_NAME = 'PuntoV'
          AND NOT EXISTS (
              SELECT 1
              FROM INFORMATION_SCHEMA.COLUMNS AS new_col
              WHERE new_col.TABLE_SCHEMA = old_col.TABLE_SCHEMA
                AND new_col.TABLE_NAME = old_col.TABLE_NAME
                AND new_col.COLUMN_NAME = 'ULocale'
          );
    DECLARE CONTINUE HANDLER FOR NOT FOUND SET finished = 1;

    OPEN column_cursor;
    rename_loop: LOOP
        FETCH column_cursor INTO table_name_value;
        IF finished = 1 THEN
            LEAVE rename_loop;
        END IF;

        SET @rename_sql = CONCAT(
            'ALTER TABLE `', REPLACE(DATABASE(), '`', '``'), '`.`',
            REPLACE(table_name_value, '`', '``'),
            '` RENAME COLUMN `PuntoV` TO `ULocale`'
        );
        PREPARE rename_statement FROM @rename_sql;
        EXECUTE rename_statement;
        DEALLOCATE PREPARE rename_statement;
    END LOOP;
    CLOSE column_cursor;
END//
CALL RenamePuntoVColumnsToULocale()//
DROP PROCEDURE RenamePuntoVColumnsToULocale//
DELIMITER ;

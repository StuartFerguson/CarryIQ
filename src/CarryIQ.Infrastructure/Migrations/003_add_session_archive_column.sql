ALTER TABLE PracticeSessions
    ADD COLUMN IF NOT EXISTS IsArchived BOOLEAN;

UPDATE PracticeSessions
SET IsArchived = FALSE
WHERE IsArchived IS NULL;

CREATE INDEX IF NOT EXISTS IX_PracticeSessions_GolferProfileId_IsArchived_SessionDate
    ON PracticeSessions (GolferProfileId, IsArchived, SessionDate);

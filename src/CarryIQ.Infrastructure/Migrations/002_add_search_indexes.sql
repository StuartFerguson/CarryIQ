CREATE INDEX IF NOT EXISTS IX_Clubs_GolferProfileId_IsActive_SortOrder
    ON Clubs (GolferProfileId, IsActive, SortOrder);

CREATE INDEX IF NOT EXISTS IX_PracticeSessions_GolferProfileId_SessionDate
    ON PracticeSessions (GolferProfileId, SessionDate);

CREATE INDEX IF NOT EXISTS IX_Shots_PracticeSessionId_ShotSequence
    ON Shots (PracticeSessionId, ShotSequence);

INSERT INTO SchemaVersion (Version, AppliedAtUtc)
SELECT 2, CURRENT_TIMESTAMP
WHERE NOT EXISTS (SELECT 1 FROM SchemaVersion WHERE Version = 2);

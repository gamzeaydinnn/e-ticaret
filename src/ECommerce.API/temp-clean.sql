DELETE FROM RefreshTokens WHERE UserId IN (SELECT Id FROM Users WHERE Email IN ('admin@local', 'admin@admin.com'));
DELETE FROM AspNetUserRoles WHERE UserId IN (SELECT Id FROM Users WHERE Email IN ('admin@local', 'admin@admin.com'));
DELETE FROM Users WHERE Email IN ('admin@local', 'admin@admin.com');
DELETE FROM AspNetUsers WHERE Email IN ('admin@local', 'admin@admin.com');

-- Demo kurye hesabını aktif et
-- ahmet@courier.com

-- 1. User'ı aktif et
UPDATE Users 
SET IsActive = 1, 
    EmailConfirmed = 1,
    UpdatedAt = GETUTCDATE()
WHERE Email = 'ahmet@courier.com';

-- 2. Courier'ı aktif et
UPDATE Couriers
SET IsActive = 1,
    UpdatedAt = GETUTCDATE()
WHERE UserId IN (SELECT Id FROM Users WHERE Email = 'ahmet@courier.com');

-- 3. Kontrol et
SELECT 
    u.Id as UserId,
    u.Email,
    u.FullName,
    u.IsActive as UserIsActive,
    u.Role,
    c.Id as CourierId,
    c.IsActive as CourierIsActive,
    c.Status,
    c.Phone
FROM Users u
LEFT JOIN Couriers c ON c.UserId = u.Id
WHERE u.Email = 'ahmet@courier.com';

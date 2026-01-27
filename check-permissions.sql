-- CustomerSupport rolünden Products.View ve Categories.View izinlerini kaldır
-- Önce mevcut durumu kontrol et

SELECT 
    r.Name as RoleName, 
    rc.ClaimType, 
    rc.ClaimValue 
FROM AspNetRoleClaims rc
JOIN AspNetRoles r ON rc.RoleId = r.Id
WHERE r.Name = 'CustomerSupport' 
AND rc.ClaimType = 'Permission'
ORDER BY rc.ClaimValue;

$password = '484801'
$md5 = New-Object System.Security.Cryptography.MD5CryptoServiceProvider
$bytes = [System.Text.Encoding]::UTF8.GetBytes($password)
$hash = $md5.ComputeHash($bytes)
$hashString = -join ($hash | ForEach-Object { $_.ToString('x2') })
Write-Output $hashString

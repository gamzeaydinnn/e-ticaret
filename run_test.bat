@echo off
C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe -ExecutionPolicy Bypass -NoProfile -Command ^
  "$today = Get-Date -Format 'yyyy-MM-dd'; " ^
  "$password = 'ZeMe@48.golkoy2'; " ^
  "$dataToHash = \"$today $password\"; " ^
  "$md5 = [System.Security.Cryptography.MD5]::Create(); " ^
  "$bytes = [System.Text.Encoding]::UTF8.GetBytes($dataToHash); " ^
  "$hashBytes = $md5.ComputeHash($bytes); " ^
  "$hash = ($hashBytes | ForEach-Object { $_.ToString('x2') }) -join ''; " ^
  "Write-Output \"Tarih: $today\"; " ^
  "Write-Output \"Hash String: $dataToHash\"; " ^
  "Write-Output \"MD5 Hash: $hash\";" > c:\Users\GAMZE\Desktop\eticaret\test_result.txt 2>&1

type c:\Users\GAMZE\Desktop\eticaret\test_result.txt

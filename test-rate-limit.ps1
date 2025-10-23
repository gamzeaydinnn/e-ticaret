# test-rate-limit.ps1
# Single-request checks + robust multi-request loop. Outputs to rate-test.log

Add-Type -AssemblyName System.Net.Http
$client = [System.Net.Http.HttpClient]::new()

# Single-request tests for different host forms
$healthUrls = @("http://localhost:5153/api/health","http://127.0.0.1:5153/api/health","http://[::1]:5153/api/health")
foreach ($u in $healthUrls) {
    Write-Host "Testing $u"
    try {
        $r = $client.GetAsync($u).GetAwaiter().GetResult()
        Write-Host " -> $($r.StatusCode)"
        $body = $r.Content.ReadAsStringAsync().GetAwaiter().GetResult()
        if ($body) { Write-Host $body }
    } catch {
        # Use formatted string to avoid PowerShell parsing $u: as a variable scope
        Write-Host ("Exception for {0}:" -f $u)
        $_.Exception | Format-List * -Force
    }
}

# Loop test (150 requests) and capture full exception info when present
$outfile = ".\rate-test.log"
"Run at $(Get-Date)" | Out-File $outfile
$url = 'http://localhost:5153/api/products'
$detailed500 = 0
for ($i = 1; $i -le 150; $i++) {
    try {
        $resp = $client.GetAsync($url).GetAwaiter().GetResult()
        $code = [int]$resp.StatusCode
        if ($code -ge 500) {
            # capture body for the first few 500 responses to inspect the server error
            $body = $resp.Content.ReadAsStringAsync().GetAwaiter().GetResult()
            $safeBody = if ($body) { ($body -replace "\r?\n", ' ') } else { '' }
            if ($detailed500 -lt 5) {
                $line = "{0,3} -> {1} | BODY: {2}" -f $i, $code, $safeBody
                $detailed500++
            } else {
                $line = "{0,3} -> {1}" -f $i, $code
            }
        } else {
            $line = "{0,3} -> {1}" -f $i, $code
        }
    } catch {
        $ex = $_.Exception
        $msg = $ex.Message
        if ($ex.InnerException) { $msg += " | Inner: " + $ex.InnerException.Message }
        $msg += " | HResult: " + ($ex.HResult)
        $line = "{0,3} -> ERR: {1}" -f $i, $msg
    }
    $line | Tee-Object -FilePath $outfile -Append
    Write-Host $line
    Start-Sleep -Milliseconds 50
}

Write-Host "Done. Results appended to $outfile"
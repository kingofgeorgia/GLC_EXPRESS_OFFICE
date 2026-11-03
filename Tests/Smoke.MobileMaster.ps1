param([string]$BaseUrl = "http://localhost:52722")
$ErrorActionPreference = "Stop"
$agents = @{
    Desktop = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/130.0.0.0 Safari/537.36"
    iPhone = "Mozilla/5.0 (iPhone; CPU iPhone OS 18_0 like Mac OS X) AppleWebKit/605.1.15 Version/18.0 Mobile/15E148 Safari/604.1"
    Android = "Mozilla/5.0 (Linux; Android 14; Pixel 8) AppleWebKit/537.36 Chrome/130.0.0.0 Mobile Safari/537.36"
}
foreach ($entry in $agents.GetEnumerator()) {
    $response = Invoke-WebRequest ($BaseUrl.TrimEnd('/') + '/') -UserAgent $entry.Value -UseBasicParsing
    foreach ($required in @('Content/bootstrap.css', 'Content/Site.css', 'Content/home.css', 'Scripts/home.js', 'id="siteNavigation"', 'class="glc-home"')) {
        if (!$response.Content.Contains($required)) { throw "$($entry.Key): missing $required" }
    }
    if ($response.Content.Contains('Mobile Master Page')) { throw "$($entry.Key): legacy mobile master was selected" }
    Write-Output "$($entry.Key): responsive master OK (HTTP $($response.StatusCode))"
}

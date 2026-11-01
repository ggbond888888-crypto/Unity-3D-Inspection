# CORS 验证脚本：直连后端校验响应头，不依赖浏览器。
# 用法：powershell -ExecutionPolicy Bypass -File scripts/cors-check.ps1 [-BaseUrl http://127.0.0.1:8080]

param(
    [string]$BaseUrl = "http://127.0.0.1:8080",
    [string]$Origin = "http://localhost:5173"
)

$script:failures = 0

function Check($condition, $message) {
    if ($condition) {
        Write-Host "[PASS] $message" -ForegroundColor Green
    } else {
        Write-Host "[FAIL] $message" -ForegroundColor Red
        $script:failures++
    }
}

# 发送请求并返回状态码与响应头；4xx/5xx 也作为正常结果返回，便于断言。
function Send-Request {
    param(
        [string]$Method,
        [string]$Path,
        [hashtable]$Headers = @{},
        [string]$JsonBody
    )

    $req = [System.Net.HttpWebRequest]::Create("$BaseUrl$Path")
    $req.Method = $Method
    $req.AllowAutoRedirect = $false
    foreach ($key in $Headers.Keys) { $req.Headers[$key] = $Headers[$key] }

    if ($JsonBody) {
        $bytes = [System.Text.Encoding]::UTF8.GetBytes($JsonBody)
        $req.ContentType = "application/json; charset=utf-8"
        $req.ContentLength = $bytes.Length
        $stream = $req.GetRequestStream()
        $stream.Write($bytes, 0, $bytes.Length)
        $stream.Close()
    }

    try {
        $resp = $req.GetResponse()
        $result = [pscustomobject]@{ Status = [int]$resp.StatusCode; Headers = $resp.Headers }
        $resp.Close()
        return $result
    } catch [System.Net.WebException] {
        $resp = $_.Exception.Response
        if ($null -eq $resp) { throw }
        $result = [pscustomobject]@{ Status = [int]$resp.StatusCode; Headers = $resp.Headers }
        $resp.Close()
        return $result
    }
}

Write-Host "目标后端: $BaseUrl    模拟来源: $Origin`n" -ForegroundColor Cyan

# 1. POST 预检：应返回 204，并带 Allow-Origin / Allow-Methods / Allow-Headers
$preflightPost = Send-Request -Method "OPTIONS" -Path "/issues" -Headers @{
    "Origin"                        = $Origin
    "Access-Control-Request-Method" = "POST"
    "Access-Control-Request-Headers" = "content-type"
}
Check ($preflightPost.Status -eq 204) "POST /issues 预检状态码为 204（实际 $($preflightPost.Status)）"
Check ($preflightPost.Headers["Access-Control-Allow-Origin"] -eq "*") "预检带 Access-Control-Allow-Origin: *（实际 '$($preflightPost.Headers['Access-Control-Allow-Origin'])'）"
$allowedMethods = [string]$preflightPost.Headers["Access-Control-Allow-Methods"]
Check ($allowedMethods -match "POST") "允许 POST（实际 '$allowedMethods'）"
Check ($allowedMethods -match "GET") "允许 GET（实际 '$allowedMethods'）"
Check ($allowedMethods -match "PATCH") "允许 PATCH（实际 '$allowedMethods'）"
$allowedHeaders = [string]$preflightPost.Headers["Access-Control-Allow-Headers"]
Check ($allowedHeaders -match "(?i)content-type") "允许 Content-Type 请求头（实际 '$allowedHeaders'）"

# 2. PATCH 预检：确认修改状态的请求也在白名单内
$preflightPatch = Send-Request -Method "OPTIONS" -Path "/issues/any-id/status" -Headers @{
    "Origin"                        = $Origin
    "Access-Control-Request-Method" = "PATCH"
    "Access-Control-Request-Headers" = "content-type"
}
Check ($preflightPatch.Status -eq 204) "PATCH /issues/:id/status 预检状态码为 204（实际 $($preflightPatch.Status)）"
Check ($preflightPatch.Headers["Access-Control-Allow-Origin"] -eq "*") "PATCH 预检带 Access-Control-Allow-Origin: *"

# 3. 实际 GET 请求：简单请求也必须带 Allow-Origin
$getHealth = Send-Request -Method "GET" -Path "/health" -Headers @{ "Origin" = $Origin }
Check ($getHealth.Status -eq 200) "GET /health 返回 200（实际 $($getHealth.Status)）"
Check ($getHealth.Headers["Access-Control-Allow-Origin"] -eq "*") "GET /health 带 Access-Control-Allow-Origin: *"

$getIssues = Send-Request -Method "GET" -Path "/issues" -Headers @{ "Origin" = $Origin }
Check ($getIssues.Status -eq 200) "GET /issues 返回 200（实际 $($getIssues.Status)）"
Check ($getIssues.Headers["Access-Control-Allow-Origin"] -eq "*") "GET /issues 带 Access-Control-Allow-Origin: *"

# 4. 实际 POST（空标题，预期 400，不写入数据）：确认错误响应同样带 Allow-Origin
$postInvalid = Send-Request -Method "POST" -Path "/issues" -Headers @{ "Origin" = $Origin } -JsonBody '{"title":"","priority":"low"}'
Check ($postInvalid.Status -eq 400) "POST /issues 空标题返回 400（实际 $($postInvalid.Status)）"
Check ($postInvalid.Headers["Access-Control-Allow-Origin"] -eq "*") "POST 的 400 响应带 Access-Control-Allow-Origin: *"

# 5. 实际 PATCH（非法状态，预期 400，不写入数据）：确认错误响应同样带 Allow-Origin
$patchInvalid = Send-Request -Method "PATCH" -Path "/issues/any-id/status" -Headers @{ "Origin" = $Origin } -JsonBody '{"status":"done"}'
Check ($patchInvalid.Status -eq 400) "PATCH 非法 status 返回 400（实际 $($patchInvalid.Status)）"
Check ($patchInvalid.Headers["Access-Control-Allow-Origin"] -eq "*") "PATCH 的 400 响应带 Access-Control-Allow-Origin: *"

Write-Host ""
if ($script:failures -eq 0) {
    Write-Host "全部 CORS 校验通过（0 项失败）" -ForegroundColor Green
    exit 0
}
Write-Host "存在 $($script:failures) 项失败" -ForegroundColor Red
exit 1

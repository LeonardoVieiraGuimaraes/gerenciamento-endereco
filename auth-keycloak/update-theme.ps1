$ErrorActionPreference = "Stop"

Write-Host "Autenticando como Admin..."
$body = @{
    client_id = "admin-cli"
    username = "admin"
    password = "admin123"
    grant_type = "password"
}
$tokenResponse = Invoke-RestMethod -Uri "http://localhost:8089/realms/master/protocol/openid-connect/token" -Method Post -Body $body
$token = $tokenResponse.access_token

$headers = @{
    Authorization = "Bearer $token"
}

Write-Host "Obtendo dados do realm..."
$realm = Invoke-RestMethod -Uri "http://localhost:8089/admin/realms/gerenciamento-endereco" -Method Get -Headers $headers

Write-Host "Atualizando accountTheme e loginTheme para 'gerenciamento-endereco'..."
$realm.accountTheme = "gerenciamento-endereco"
$realm.loginTheme = "gerenciamento-endereco"

$jsonPayload = $realm | ConvertTo-Json -Depth 10

Invoke-RestMethod -Uri "http://localhost:8089/admin/realms/gerenciamento-endereco" -Method Put -Headers $headers -Body $jsonPayload -ContentType "application/json"

Write-Host "Sucesso! Tema da conta (Account Theme) configurado no Keycloak para 'gerenciamento-endereco'."

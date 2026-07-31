$ErrorActionPreference = "Stop"

Write-Host "1. Autenticando na API Admin do Keycloak..."
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

# --- CONFIGURAR REALM MASTER ---
Write-Host "2. Configurando i18n e Temas no Realm MASTER..."
$master = Invoke-RestMethod -Uri "http://localhost:8089/admin/realms/master" -Method Get -Headers $headers
$master | Add-Member -NotePropertyName "internationalizationEnabled" -NotePropertyValue $true -Force
$master | Add-Member -NotePropertyName "supportedLocales" -NotePropertyValue @("pt-BR") -Force
$master | Add-Member -NotePropertyName "defaultLocale" -NotePropertyValue "pt-BR" -Force
$master | Add-Member -NotePropertyName "adminTheme" -NotePropertyValue "gerenciamento-endereco" -Force
$master | Add-Member -NotePropertyName "emailTheme" -NotePropertyValue "gerenciamento-endereco" -Force
$master | Add-Member -NotePropertyName "loginTheme" -NotePropertyValue "gerenciamento-endereco" -Force
$master | Add-Member -NotePropertyName "accountTheme" -NotePropertyValue "gerenciamento-endereco" -Force

$masterJson = $master | ConvertTo-Json -Depth 10
Invoke-RestMethod -Uri "http://localhost:8089/admin/realms/master" -Method Put -Headers $headers -Body $masterJson -ContentType "application/json"
Write-Host "-> Realm MASTER atualizado com sucesso."

# --- CONFIGURAR REALM GERENCIAMENTO-ENDERECO ---
Write-Host "3. Configurando i18n e Temas no Realm GERENCIAMENTO-ENDERECO..."
$realm = Invoke-RestMethod -Uri "http://localhost:8089/admin/realms/gerenciamento-endereco" -Method Get -Headers $headers
$realm | Add-Member -NotePropertyName "internationalizationEnabled" -NotePropertyValue $true -Force
$realm | Add-Member -NotePropertyName "supportedLocales" -NotePropertyValue @("pt-BR") -Force
$realm | Add-Member -NotePropertyName "defaultLocale" -NotePropertyValue "pt-BR" -Force
$realm | Add-Member -NotePropertyName "adminTheme" -NotePropertyValue "gerenciamento-endereco" -Force
$realm | Add-Member -NotePropertyName "emailTheme" -NotePropertyValue "gerenciamento-endereco" -Force
$realm | Add-Member -NotePropertyName "loginTheme" -NotePropertyValue "gerenciamento-endereco" -Force
$realm | Add-Member -NotePropertyName "accountTheme" -NotePropertyValue "gerenciamento-endereco" -Force

$realmJson = $realm | ConvertTo-Json -Depth 10
Invoke-RestMethod -Uri "http://localhost:8089/admin/realms/gerenciamento-endereco" -Method Put -Headers $headers -Body $realmJson -ContentType "application/json"
Write-Host "-> Realm GERENCIAMENTO-ENDERECO atualizado com sucesso."

# --- SALVAR ARQUIVO JSON DE FORMA LIMPA ---
Write-Host "4. Exportando JSON limpo para gerenciamento-endereco-realm.json..."
[System.IO.File]::WriteAllText("d:\GitHub\ProjetosWeb\gerenciamento-endereco\auth\realm\gerenciamento-endereco-realm.json", $realmJson)
Write-Host "-> Arquivo gerenciamento-endereco-realm.json salvo com sucesso!"

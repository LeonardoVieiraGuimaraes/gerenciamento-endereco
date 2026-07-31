$ErrorActionPreference = "Stop"

Write-Host "1. Autenticando..."
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

Write-Host "2. Buscando leonardoadmin..."
$users = Invoke-RestMethod -Uri "http://localhost:8089/admin/realms/gerenciamento-endereco/users?username=leonardoadmin" -Method Get -Headers $headers
if ($users.Count -eq 0) {
    Write-Host "Usuário leonardoadmin não encontrado!"
    exit 1
}
$leonardoId = $users[0].id
Write-Host "leonardoadmin ID: $leonardoId"

Write-Host "3. Buscando cliente realm-management..."
$clients = Invoke-RestMethod -Uri "http://localhost:8089/admin/realms/gerenciamento-endereco/clients?clientId=realm-management" -Method Get -Headers $headers
$clientId = $clients[0].id

Write-Host "4. Buscando roles..."
$roles = Invoke-RestMethod -Uri "http://localhost:8089/admin/realms/gerenciamento-endereco/clients/$clientId/roles" -Method Get -Headers $headers
$manageUsers = $roles | Where-Object name -eq "manage-users"
$viewUsers = $roles | Where-Object name -eq "view-users"

Write-Host "5. Atribuindo roles ao leonardoadmin..."
$rolesToAdd = @($manageUsers, $viewUsers) | ConvertTo-Json -Depth 10

# A API espera um array de objetos RoleRepresentation
Invoke-RestMethod -Uri "http://localhost:8089/admin/realms/gerenciamento-endereco/users/$leonardoId/role-mappings/clients/$clientId" -Method Post -Headers $headers -Body $rolesToAdd -ContentType "application/json"

Write-Host "Sucesso! Roles atribuídas."

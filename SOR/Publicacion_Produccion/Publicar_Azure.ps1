<#
    .SYNOPSIS
    Script de Despliegue Automatizado a Microsoft Azure (App Service & SQL Database)
    Sistema de Gestión Interna OCC Rep Dom (SOR)
#>

Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "   DESPLIEGUE AUTOMATIZADO A MICROSOFT AZURE (SOR)      " -ForegroundColor Yellow
Write-Host "========================================================" -ForegroundColor Cyan

$CurrentDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$DownloadsDir = [System.IO.Path]::Combine($env:USERPROFILE, "Downloads")

# 1. Buscar archivo de perfil de publicación (.PublishSettings)
$SettingsFile = Get-ChildItem -Path $DownloadsDir, $CurrentDir -Filter "*.PublishSettings" -ErrorAction SilentlyContinue | Select-Object -First 1

if ($SettingsFile) {
    Write-Host "[OK] Perfil de publicacion encontrado: $($SettingsFile.FullName)" -ForegroundColor Green
    [xml]$xml = Get-Content $SettingsFile.FullName
    $profile = $xml.publishData.publishProfile | Where-Object { $_.publishMethod -eq "MSDeploy" }
    
    if (-not $profile) {
        $profile = $xml.publishData.publishProfile | Select-Object -First 1
    }

    $appName = $profile.msdeploySite
    $userName = $profile.userName
    $password = $profile.userPWD
    $publishUrl = $profile.publishUrl

    Write-Host "Destino Azure: $appName ($publishUrl)" -ForegroundColor Cyan

    # Empacar en ZIP para Kudu Deploy
    $zipPath = Join-Path $env:TEMP "SOR_Deploy.zip"
    if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
    
    Write-Host "Empacando archivos de produccion..." -ForegroundColor Yellow
    Compress-Archive -Path "$CurrentDir\*" -DestinationPath $zipPath -Force

    Write-Host "Subiendo paquete a Azure App Service..." -ForegroundColor Yellow
    $base64Auth = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("${userName}:${password}"))
    $kuduUrl = "https://$($profile.publishUrl.Split(':')[0])/api/zipdeploy"
    
    try {
        $response = Invoke-RestMethod -Uri $kuduUrl `
            -Headers @{ Authorization = "Basic $base64Auth" } `
            -Method POST `
            -InFile $zipPath `
            -ContentType "application/zip" `
            -TimeoutSec 300

        Write-Host "========================================================" -ForegroundColor Green
        Write-Host "   ¡DESPLIEGUE COMPLETADO EXITOSAMENTE EN AZURE!       " -ForegroundColor Green
        Write-Host "   URL: https://$appName.azurewebsites.net              " -ForegroundColor Cyan
        Write-Host "========================================================" -ForegroundColor Green
    }
    catch {
        Write-Host "Error en el despliegue automatico: $_" -ForegroundColor Red
        Write-Host "Puedes publicar desde Visual Studio con el perfil .PublishSettings" -ForegroundColor Yellow
    }
} else {
    Write-Host "[INFO] No se encontro archivo .PublishSettings en Descargas." -ForegroundColor Yellow
    Write-Host "Para desplegar en 1 solo clic:" -ForegroundColor Cyan
    Write-Host "1. En el portal de Azure entra a tu App Service 'SOR'" -ForegroundColor White
    Write-Host "2. Clic en 'Descargar perfil de publicacion' (Get publish profile)" -ForegroundColor White
    Write-Host "3. Vuelve a ejecutar este script." -ForegroundColor Green
}

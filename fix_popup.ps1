$filePath = "d:\Wind_inspection\WindBladeInspector.Web\Components\Pages\Inspection.razor"
$content = Get-Content -Path $filePath
$newContent = $content | Where-Object { $_ -notmatch '_showDefectPopup' }
Set-Content -Path $filePath -Value $newContent
Write-Host "Fixed the file"

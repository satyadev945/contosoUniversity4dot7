# Renders Kubernetes manifests: replaces {{PLACEHOLDER}} values using environment variables.
param(
    [Parameter(Mandatory = $true)][string]$Source,
    [Parameter(Mandatory = $true)][string]$Destination
)
$ErrorActionPreference = 'Stop'
New-Item -ItemType Directory -Force -Path $Destination | Out-Null
$map = @{
    '{{IMAGE_URI}}'    = $env:IMAGE_URI
    '{{INGRESS_HOST}}' = $env:INGRESS_HOST
    '{{NAMESPACE}}'    = $env:NAMESPACE
}
Get-ChildItem -Path $Source -Filter *.yaml | ForEach-Object {
    $text = Get-Content -Raw -Path $_.FullName
    foreach ($key in $map.Keys) {
        $text = $text.Replace($key, [string]$map[$key])
    }
    Set-Content -Path (Join-Path $Destination $_.Name) -Value $text -NoNewline
}

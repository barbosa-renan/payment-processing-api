param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('dev', 'hml', 'prod')]
    [string]$Environment,

    [Parameter(Mandatory = $false)]
    [string]$Location = 'brazilsouth',

    [Parameter(Mandatory = $false)]
    [switch]$WhatIf
)

$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$mainTemplate = Join-Path $scriptDir 'main.bicep'
$paramsFile = Join-Path $scriptDir "params/$Environment.bicepparam"

if (-not (Test-Path $mainTemplate)) {
    throw "Template nao encontrado: $mainTemplate"
}

if (-not (Test-Path $paramsFile)) {
    throw "Arquivo de parametros nao encontrado: $paramsFile"
}

$subscriptionId = az account show --query id -o tsv
if (-not $subscriptionId) {
    throw 'Nao foi possivel obter subscription ativa. Execute az login antes.'
}

$sqlPassword = Read-Host -Prompt 'Informe a senha do administrador SQL' -AsSecureString
$sqlPasswordPlain = [System.Net.NetworkCredential]::new('', $sqlPassword).Password

$baseArgs = @(
    'deployment', 'sub', 'create',
    '--location', $Location,
    '--template-file', $mainTemplate,
    '--parameters', $paramsFile,
    '--parameters', "sqlAdministratorPassword=$sqlPasswordPlain"
)

if ($WhatIf) {
    Write-Host 'Executando what-if...'
    az deployment sub what-if --location $Location --template-file $mainTemplate --parameters $paramsFile --parameters "sqlAdministratorPassword=$sqlPasswordPlain"
}
else {
    Write-Host "Executando deploy para ambiente: $Environment"
    az @baseArgs
}

Write-Host 'Concluido.'

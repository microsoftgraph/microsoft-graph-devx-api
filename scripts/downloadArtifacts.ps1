param(
    [int]$buildId = -1
)

# get script directory
$scriptsDirectory = Split-Path -Parent $MyInvocation.MyCommand.Definition

$projectPath = Split-Path -Parent $scriptsDirectory

$organization = "https://dev.azure.com/microsoftgraph"
$project = "Graph Developer Experiences"
$pipelineName = "Snippet Generation DevX"

if ($buildId -eq -1) {
    Write-Warning "selecting the latest scheduled build as buildId is not specified"
    $buildId = az pipelines build list --organization $organization --project $project --query "[?definition.name=='$pipelineName' && status=='completed' && reason=='schedule'].id" --output tsv | Select-Object -First 1
}

$artifactExtractDirectory = Join-Path $($env:WORKING_DIRECTORY) "artifact-extract-directory"
New-Item -ItemType Directory -Force -Path $artifactExtractDirectory
$stableDownloadUrl = "$organization/$project/_apis/build/builds/$buildId/artifacts?artifactName=Generation%20Test%20Results&api-version=5.0&%24format=zip"
$DownloadFolderName = "SnippetGenerationTestResults"
$downloadZipFullPath = Join-Path $artifactExtractDirectory "$DownloadFolderName.zip"
$downloadExtractDirectory = "$($env:ARTIFACTS_DIRECTORY)/TestResults"

if ([string]::IsNullOrEmpty($env:AZURE_DEVOPS_EXT_PAT))
{
    Write-Warning "Fetching request token from logged in az account user"
    $token = az account get-access-token --query accessToken --output tsv
    $auth_header_value = New-Object System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", $token)
}
else{
    $PAT_token = $env:AZURE_DEVOPS_EXT_PAT
    $base64_PAT_token = [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes(":$($PAT_token)"))
    $auth_header_value = New-Object System.Net.Http.Headers.AuthenticationHeaderValue("Basic", $base64_PAT_token)
}

try
{
    Invoke-WebRequest -Uri $stableDownloadUrl -OutFile $downloadZipFullPath -Headers @{Authorization=$auth_header_value}
}
catch
{
    Write-Warning "Failed to download artifact $DownloadFolderName"
    exit
}

Expand-Archive -Path $downloadZipFullPath -DestinationPath $downloadExtractDirectory

Remove-Item $downloadZipFullPath

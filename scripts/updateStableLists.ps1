param(
    [int]$buildId = -1
)

# get script directory
$scriptsDirectory = Split-Path -Parent $MyInvocation.MyCommand.Definition
$filterTestsScriptPath = Join-Path $scriptsDirectory "filterTests.ps1"

$projectPath = Split-Path -Parent $scriptsDirectory
$codeSnippetPipelineDirectory = Join-Path $projectPath "CodeSnippetsPipeline.Test";
$testListsDirectory = Join-Path $codeSnippetPipelineDirectory "GenerationStableTestLists"

$organization = "https://dev.azure.com/microsoftgraph"
$project = "Graph Developer Experiences"
$pipelineName = "Snippet Generation DevX"

if ($buildId -eq -1) {
    Write-Warning "selecting the latest scheduled build as buildId is not specified"
    $buildId = az pipelines build list --organization $organization --project $project --query "[?definition.name=='$pipelineName' && status=='completed' && reason=='schedule'].id" --output tsv | Select-Object -First 1
}

# if $env:TEMP is not defined, assume github action running and read from $env:RUNNER_TEMP
if ($null -eq $env:TEMP) {
    $tempFolder = $env:RUNNER_TEMP
}
else{
    $tempFolder = $env:TEMP
}
$artifactExtractDirectory = Join-Path $tempFolder "artifact-extract-directory"
New-Item -ItemType Directory -Force -Path $artifactExtractDirectory
$stableDownloadUrl = "$organization/$project/_apis/build/builds/$buildId/artifacts?artifactName=Generation%20Test%20Results&api-version=5.0&%24format=zip"
$DownloadFolderName = "SnippetGenerationTestResults"
$downloadZipFullPath = Join-Path $artifactExtractDirectory "$DownloadFolderName.zip"
$downloadExtractDirectory = $downloadZipFullPath -Replace ".zip", ""

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
    continue
}

Expand-Archive -Path $downloadZipFullPath -DestinationPath $downloadExtractDirectory

# enumerate all testlists in TestLists directory
$testListFileNames = Get-ChildItem -Path $testListsDirectory -Filter "*.testlist" -Recurse | Select-Object -ExpandProperty Name
foreach ($testListFileName in $testListFileNames)
{
    $testSuiteNameInTestListFileName = $testListFileName -replace ".testlist", ""
    $testSuiteNameInTestListFileName = $testSuiteNameInTestListFileName -replace "-v1", " v1 "
    $testSuiteNameInTestListFileName = $testSuiteNameInTestListFileName -replace "-beta", " beta "
    $stableTestSuiteNameInArtifact = $testSuiteNameInTestListFileName + "Stable TestResults"

    $stableTrxFile = Get-ChildItem -Path $downloadExtractDirectory -Filter "$stableTestSuiteNameInArtifact.trx" -Recurse | Select-Object -First 1
    $stableTxtFile = $stableTrxFile -Replace ".trx", ".txt"
    .$filterTestsScriptPath -trxFilePath $stableTrxFile.FullName -outcome "Passed" -txtOutputFilePath $stableTxtFile
    $stablePassedTests = Get-Content $stableTxtFile

    $knownIssuesFileNameInArtifact = $testSuiteNameInTestListFileName + "KnownIssues TestResults"

    $knownIssuesTrxFile = Get-ChildItem -Path $downloadExtractDirectory -Filter "$knownIssuesFileNameInArtifact.trx" -Recurse | Select-Object -First 1
    $knownIssuesTxtFile = $knownIssuesTrxFile -Replace ".trx", ".txt"
    .$filterTestsScriptPath -trxFilePath $knownIssuesTrxFile.FullName -outcome "Passed" -txtOutputFilePath $knownIssuesTxtFile
    $knownIssuesPassedTests = Get-Content $knownIssuesTxtFile

    Write-Warning "Number of passed tests from $testSuiteNameInTestListFileName stable run: $($stablePassedTests.Count)"
    Write-Warning "Number of passed tests from $testSuiteNameInTestListFileName known issues run: $($knownIssuesPassedTests.Count)"

    $passedTests = $stablePassedTests + $knownIssuesPassedTests |
    Sort-Object |
    Get-Unique
    Set-Content (Join-Path $testListsDirectory $testListFileName) $passedTests
}
Remove-Item $downloadZipFullPath
Remove-Item $downloadExtractDirectory -Recurse

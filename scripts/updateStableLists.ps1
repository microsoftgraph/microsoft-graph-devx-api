param(
    [int]$buildId = -1
)

# get script directory
$scriptsDirectory = Split-Path -Parent $MyInvocation.MyCommand.Definition
$filterTestsScriptPath = Join-Path $scriptsDirectory "filterTests.ps1"

$projectPath = Split-Path -Parent $scriptsDirectory
$codeSnippetPipelineDirectory = Join-Path $projectPath "CodeSnippetPipeline.Test";
$testListsDirectory = Join-Path ($codeSnippetPipelineDirectory, "GenerationStableTestLists")

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

# enumerate all testlists in TestLists directory
$testListFileNames = Get-ChildItem -Path $testListsDirectory -Filter "*.testlist" -Recurse | Select-Object -ExpandProperty Name

foreach ($testListFileName in $testListFileNames)
{
    $testSuiteNameInTestListFileName = $testListFileName -replace ".testlist", ""

    $testSuiteNameInArtifact = $testSuiteNameInTestListFileName -replace "-v1-", "-v1.0-"
    $stableTestSuiteNameInArtifact = $testSuiteNameInArtifact + "Stable"
    $stableDownloadUrl = "$organization/$project/_apis/build/builds/$buildId/artifacts?artifactName=$($testSuiteNameInArtifact)Stable%20Test%20Results&api-version=5.0&%24format=zip"
    $stableZipFullPath = Join-Path $tempFolder "$stableTestSuiteNameInArtifact.zip"

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
        Invoke-WebRequest -Uri $stableDownloadUrl -OutFile $stableZipFullPath -Headers @{Authorization=$auth_header_value}
    }
    catch
    {
        Write-Warning "Failed to download artifact $stableTestSuiteNameInArtifact"
        continue
    }

    $stableExtractDirectory = $stableZipFullPath -Replace ".zip", ""
    Expand-Archive -Path $stableZipFullPath -DestinationPath $stableExtractDirectory
    $stableTrxFile = Get-ChildItem -Path $stableExtractDirectory -Filter "*.trx" -Recurse | Select-Object -First 1
    $stableTxtFile = $stableTrxFile -Replace ".trx", ".txt"
    .$filterTestsScriptPath -trxFilePath $stableTrxFile.FullName -outcome "Passed" -txtOutputFilePath $stableTxtFile
    $stablePassedTests = Get-Content $stableTxtFile
    Remove-Item $stableZipFullPath
    Remove-Item $stableExtractDirectory -Recurse

    $knownIssuesTestSuiteNameInArtifact = $testSuiteNameInArtifact + "KnownIssues"
    $runId = 1157710
    $knownIssuesDownloadUrl = "$organization/$project/_TestManagement/Runs?runId=$runId&attachmentName=Test%20Results&api-version=5.0&%24format=zip"
    $knownIssuesZipFullPath = Join-Path $tempFolder "$knownIssuesTestSuiteNameInArtifact.zip"
    try
    {
        Invoke-WebRequest -Uri $knownIssuesDownloadUrl -OutFile $knownIssuesZipFullPath -Headers @{Authorization=$auth_header_value}
    }
    catch
    {
        Write-Warning "Failed to download artifact $knownIssuesTestSuiteNameInArtifact"
        continue
    }

    $knownIssuesExtractDirectory = $knownIssuesZipFullPath -Replace ".zip", ""
    Expand-Archive -Path $knownIssuesZipFullPath -DestinationPath $knownIssuesExtractDirectory
    $knownIssuesTrxFile = Get-ChildItem -Path $knownIssuesExtractDirectory -Filter "*.trx" -Recurse | Select-Object -First 1
    $knownIssuesTxtFile = $knownIssuesTrxFile -Replace ".trx", ".txt"
    .$filterTestsScriptPath -trxFilePath $knownIssuesTrxFile.FullName -outcome "Passed" -txtOutputFilePath $knownIssuesTxtFile
    $knownIssuesPassedTests = Get-Content $knownIssuesTxtFile
    Remove-Item $knownIssuesZipFullPath
    Remove-Item $knownIssuesExtractDirectory -Recurse

    Write-Warning "Number of passed tests from stable run: $($stablePassedTests.Count)"
    Write-Warning "Number of passed tests from known issues run: $($knownIssuesPassedTests.Count)"

    $passedTests = $stablePassedTests + $knownIssuesPassedTests |
        Sort-Object |
        ForEach-Object { $_.Replace("-executes", "").Replace("-compiles", "") } |
        Get-Unique
    Set-Content (Join-Path $testListsDirectory $testListFileName) $passedTests
}

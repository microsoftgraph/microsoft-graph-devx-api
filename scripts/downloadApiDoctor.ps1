# this script downloads API doctor as it is not published as a tool.
# it relies on the ApiDoctorRef project to keep tracks of the latest version. (dependbot creates PRs on that project)

[xml]$csproj = Get-Content -Path "./ApiDoctorRef/ApiDoctorRef.csproj"

$version = $csproj.Project.ItemGroup.PackageReference.Version

$targetFile = [System.IO.Path]::GetTempPath() + "ApiDoctor.zip"
$targetDirectory = $env:APIDOCTORPATH
if($null -eq $targetDirectory -or $targetDirectory -eq "") {
    Write-Information "APIDOCTORPATH environment variable is not set. Using default value."
    $targetDirectory = Join-path ([System.IO.Path]::GetTempPath()) "ApiDoctor"
}

$url = "https://globalcdn.nuget.org/packages/apidoctor.$version.nupkg"

Invoke-WebRequest -Uri $url -OutFile $targetFile

Expand-Archive -Path $targetFile -DestinationPath $targetDirectory -Verbose
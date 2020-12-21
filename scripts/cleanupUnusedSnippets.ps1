$snippetFiles = Get-ChildItem -Recurse -Filter snippets | Get-ChildItem -Recurse -Filter *.md | Select-Object -ExpandProperty FullName
$docsFiles = Get-ChildItem -Recurse -Filter *.md -Exclude */snippets/*
$resolvedSnippetsFiles = New-Object System.Collections.Generic.HashSet[string]
foreach ($docFile in $docsFiles) {
	$content = Get-Content -Path $docFile.FullName 
	if($content -cmatch ".*INCLUDE.*") {
		$pathMatches = [regex]::Matches($content, "\(([^\)]+)\)");
		foreach($pathMatch in $pathMatches) {
			$snippetPath = $pathMatch.Groups[1].Value;
			if($snippetPath -match ".*snippets.*" -and $snippetPath -cnotmatch ".*:\/\/.*") {
				$snippetFullPath = (Resolve-Path -LiteralPath (Join-Path $docFile.DirectoryName $snippetPath)).Path
				$resolvedSnippetsFiles.add($snippetFullPath) | Out-Null
			}
		}
	}
}
foreach($candidateForDeletion in $snippetFiles) {
	if($resolvedSnippetsFiles.Contains($candidateForDeletion) -eq $false) {
		Remove-Item -Path $candidateForDeletion -Verbose
	}
}
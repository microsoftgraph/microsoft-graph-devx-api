$snippetFiles = Get-ChildItem -Recurse -Filter snippets | Get-ChildItem -Recurse -Filter *.md | Select-Object -ExpandProperty FullName
$docsFiles = Get-ChildItem -Recurse -Filter *.md -Exclude */snippets/*
$resolvedSnippetsFiles = New-Object System.Collections.Generic.HashSet[string]
foreach ($docFile in $docsFiles) {
	$content = Get-Content -Path $docFile.FullName 
	if($content -cmatch ".*INCLUDE.*") { # matches this in the referencing files [!INCLUDE [sample-code](../includes/snippets/csharp/get-accesspackageassignmentresourcerole-csharp-snippets.md)]
		$pathMatches = [regex]::Matches($content, "\(([^\)]+)\)"); # gets the path portion of a MD link
		foreach($pathMatch in $pathMatches) {
			$snippetPath = $pathMatch.Groups[1].Value;
			if($snippetPath -match ".*snippets.*" -and $snippetPath -cnotmatch ".*:\/\/.*" -and $snippetPath.EndsWith('.md')) { # checks that the snippet is in a "snippets" folder and that it's not a link "://"
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
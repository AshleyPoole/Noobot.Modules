Write-Host "Ensure the CSPROJ files have been updated first!"
Write-Host "Press enter in order to create and publish new package..."
Read-Host

Push-Location .\src

$moduleDirs = Get-ChildItem | ?{$_.PSISContainer}

foreach ($moduleDir in $moduleDirs){
    Push-Location $moduleDir

    Write-Host "Removing previous nuget packages"
    Remove-Item .\bin\Release\*.nupkg > $null

    Write-Host "Building $moduleDir"
    msbuild /t:pack /p:Configuration=Release

    $nugetPackage = Get-ChildItem .\bin\Release\*.nupkg | Select-Object -First 1

    Write-Host "Publishing package:$nugetPackage"
    nuget push $nugetPackage -Source https://api.nuget.org/v3/index.json

    Pop-Location
}

Pop-Location
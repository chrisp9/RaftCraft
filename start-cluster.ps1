Get-ChildItem -Path C:\RaftCraft\Log -Include *.* -File -Recurse | foreach { $_.Delete()}


start-process -FilePath 'dotnet' -ArgumentList 'run', '--no-build', 'AppConfig_node1.json'
sleep 1

start-process -FilePath 'dotnet' -ArgumentList 'run', '--no-build', 'AppConfig_node2.json'
sleep 1

start-process -FilePath 'dotnet' -ArgumentList 'run', '--no-build', 'AppConfig_node3.json'
sleep 1
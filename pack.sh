set -e
MSBuild.exe BackPanel.sln /t:Clean /p:Configuration=Release
MSBuild.exe BackPanel.sln /t:BackPanel /p:Configuration=Release
mkdir -p nupkg
rm -f nupkg/*.nupkg
(cd BackPanel && paket pack ../nupkg)
paket push --url https://www.myget.org/F/pragmatrix --endpoint /api/v2/package --api-key $MYGETAPIKEY nupkg/*.nupkg

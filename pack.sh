set -e
MSBuild.exe BackPanel.sln /t:Clean /p:Configuration=Release
MSBuild.exe BackPanel.sln /t:BackPanel /p:Configuration=Release
mkdir -p backpanel-nupkg
rm -f backpanel-nupkg/*.nupkg
(cd BackPanel && paket pack ../backpanel-nupkg)
paket push --url https://www.myget.org/F/pragmatrix --endpoint /api/v2/package --api-key $MYGETAPIKEY backpanel-nupkg/*.nupkg

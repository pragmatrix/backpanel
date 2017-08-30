set -e
MSBuild.exe BackPanel.sln /t:Clean /p:Configuration=Release
MSBuild.exe BackPanel.sln /t:BackPanel /p:Configuration=Release
(cd BackPanel && paket pack .)

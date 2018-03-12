#!/bin/bash

rm -rf ru.org.openam.iis.httpmodule/bin
msbuild /p:Configuration=Release ru.org.openam.dotnet.sln
nuget install NUnit.Runners -Version 3.8.0 -OutputDirectory testrunner
mono ./testrunner/NUnit.ConsoleRunner.3.8.0/tools/nunit3-console.exe ./ru.org.openam.nunit/bin/Release/ru.org.openam.sdk.nunit.dll
rm -rf /tmp/ru.org.openam.iis.httpmodule.zip
cp ru.org.openam.iis.httpmodule/Properties/AssemblyInfo.cs ru.org.openam.iis.httpmodule/bin/Release/ru.org.openam.version.txt
cat ru.org.openam.iis.httpmodule/bin/Release/ru.org.openam.version.txt
zip -rjv  /tmp/ru.org.openam.iis.httpmodule.zip ru.org.openam.iis.httpmodule/bin/Release/*
#scp -P 23 /tmp/ru.org.openam.iis.httpmodule.zip repo.openam.org.ru:/var/www/repo
#cat ru.org.openam.iis.httpmodule/bin/Release/ru.org.openam.version.txt
#echo http://repo.openam.org.ru/ru.org.openam.iis.httpmodule.zip


#!/bin/bash

rm -rf ru.org.openam.iis.httpmodule/bin
xbuild /p:Configuration=Release ru.org.openam.dotnet.sln
nunit-console ru.org.openam.nunit/bin/Release/ru.org.openam.sdk.nunit.dll
rm -rf /tmp/ru.org.openam.iis.httpmodule.zip
cp ru.org.openam.iis.httpmodule/Properties/AssemblyInfo.cs ru.org.openam.iis.httpmodule/bin/Release/version.txt
zip -rjv  /tmp/ru.org.openam.iis.httpmodule.zip ru.org.openam.iis.httpmodule/bin/Release/*
echo http://repo.openam.org.ru/ru.org.openam.iis.httpmodule.zip
scp -P 23 /tmp/ru.org.openam.iis.httpmodule.zip repo.openam.org.ru:/var/www/repo

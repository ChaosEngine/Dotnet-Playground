#!/bin/bash
#
docker run -it --rm -v $(pwd):/app --workdir /app microsoft/aspnetcore-build:2.0 bash -c "dotnet restore AspNetCore.ExistingDb && dotnet publish -c Release && find AspNetCore.ExistingDb/bin/Release/netcoreapp*/publish/ -type d -exec chmod u=rwx,g=rwx,o=rx {} \;; find AspNetCore.ExistingDb/bin/Release/netcoreapp*/publish/ AspNetCore.ExistingDb/wwwroot/{js,css}/ -type f -exec chmod u=rw,g=rw,o=r {} \;" \
&& docker build -t chaosengine/aspnetcore:2.0 . \
&& chmod 777 shared
#
# dotnet AspNetCore.ExistingDb.dll & sleep 2s && chmod 666 /sockets/www.sock

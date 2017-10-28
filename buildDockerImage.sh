#!/bin/bash
#
	docker run -it --rm -v $(pwd):/app --workdir /app microsoft/aspnetcore-build:latest bash -c \
		"cd Caching-MySQL && dotnet restore && cd .. && \
		dotnet test -v n AspNetCore.ExistingDb.Tests && \
		dotnet publish -c Release \
			-p:PublishWithAspNetCoreTargetManifest=false #remove this afer prerelease patch publish \
			AspNetCore.ExistingDb && \
		find AspNetCore.ExistingDb*/bin/Release/netcoreapp2.0/publish/ -type d -exec chmod ug=rwx,o=rx {} \; && \
		find AspNetCore.ExistingDb*/bin/Release/netcoreapp2.0/publish/ AspNetCore.ExistingDb/wwwroot/{js,css}/ -type f -exec chmod ug=rw,o=r {} \;" && \
	docker build -t chaosengine/aspnetcore:latest . && \
	chmod 777 shared

#
# dotnet AspNetCore.ExistingDb.dll & sleep 2s && chmod 666 /sockets/www.sock && fg

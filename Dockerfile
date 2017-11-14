FROM microsoft/aspnetcore-build:latest AS build
WORKDIR /build
COPY . .

ENV DBKind="sqlite" ConnectionStrings__Sqlite="Filename=./bin/Debug/netcoreapp2.0/Blogging.db"
RUN dotnet test -v n AspNetCore.ExistingDb.Tests
RUN dotnet publish -c Release AspNetCore.ExistingDb

RUN find AspNetCore.ExistingDb/bin/Release/netcoreapp2.0/publish/ -type d -exec chmod ug=rwx,o=rx {} \; && \
	find AspNetCore.ExistingDb/bin/Release/netcoreapp2.0/publish/ -type f -exec chmod ug=rw,o=r {} \; && \
	chmod 777 shared



FROM microsoft/aspnetcore

WORKDIR /app
COPY --from=build /build/AspNetCore.ExistingDb/bin/Release/netcoreapp2.0/publish/ /build/startApp.sh ./

ENV USER=www-data ASPNETCORE_URLS=http://+:5000
RUN	chown -R $USER:$USER .
USER "$USER"

VOLUME /shared
EXPOSE 5000

ENTRYPOINT ["./startApp.sh"]

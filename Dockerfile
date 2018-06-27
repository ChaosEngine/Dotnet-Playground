FROM microsoft/dotnet:2.1-sdk-stretch AS build
RUN curl -sL https://deb.nodesource.com/setup_8.x | bash - && \
	apt-get install -y nodejs
RUN npm i gulp@next -g
WORKDIR /build
COPY . .

ENV DBKind="sqlite" ConnectionStrings__Sqlite="Filename=./bin/Debug/netcoreapp2.1/Blogging.db"
RUN dotnet test -v n AspNetCore.ExistingDb.Tests
RUN dotnet publish -c Release \
    #-p:PublishWithAspNetCoreTargetManifest=false #remove this afer prerelease patch publish \
    AspNetCore.ExistingDb

RUN find AspNetCore.ExistingDb/bin/Release/netcoreapp2.1/publish/ -type d -exec chmod ug=rwx,o=rx {} \; && \
		find AspNetCore.ExistingDb/bin/Release/netcoreapp2.1/publish/ -type f -exec chmod ug=rw,o=r {} \; && \
		chmod 777 shared



FROM microsoft/dotnet:2.1.1-aspnetcore-runtime-stretch-slim
WORKDIR /app
COPY --from=build /build/AspNetCore.ExistingDb/bin/Release/netcoreapp2.1/publish/ /build/startApp.sh ./

ENV TZ=Europe/Warsaw
RUN ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && echo $TZ > /etc/timezone

ENV USER=www-data ASPNETCORE_URLS=http://+:5000
RUN chown -R $USER:$USER .
USER "$USER"

VOLUME /shared
EXPOSE 5000

ENTRYPOINT ["./startApp.sh"]


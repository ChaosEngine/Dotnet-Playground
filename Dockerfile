FROM microsoft/dotnet:2.1-sdk-stretch AS build
RUN curl -sL https://deb.nodesource.com/setup_8.x | bash - && \
	apt-get install -y nodejs
RUN npm i gulp@next -g
WORKDIR /build
COPY . .

ENV DBKind="sqlite" ConnectionStrings__Sqlite="Filename=./bin/Debug/netcoreapp2.1/Blogging.db"
RUN dotnet test -v n AspNetCore.ExistingDb.Tests
RUN dotnet publish -c Release -r linux-x64 \
    #-p:PublishWithAspNetCoreTargetManifest=false #remove this afer prerelease patch publish \
	/p:ShowLinkerSizeComparison=true /p:CrossGenDuringPublish=false \
    AspNetCore.ExistingDb





FROM microsoft/dotnet:2.1-runtime-deps-stretch-slim
WORKDIR /app
COPY --from=build --chown=www-data:www-data /build/AspNetCore.ExistingDb/bin/Release/netcoreapp2.1/linux-x64/publish/ /build/startApp.sh ./

ENV TZ=Europe/Warsaw USER=www-data ASPNETCORE_URLS=http://+:5000
RUN ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && echo $TZ > /etc/timezone

#RUN chown -R $USER:$USER .
USER "$USER"

VOLUME /shared
EXPOSE 5000

#ENTRYPOINT ["./startApp.sh"]
ENTRYPOINT ["./AspNetCore.ExistingDb"]

FROM microsoft/aspnetcore
WORKDIR /app

ENV ASPNETCORE_URLS http://+:5000

COPY . .
RUN dotnet restore \
	&& dotnet build -c Release \
	&& dotnet publish --output /out/ --configuration Release

#EXPOSE 5000
#WORKDIR /out

#ENTRYPOINT ["dotnet", " ", "app.dll"]

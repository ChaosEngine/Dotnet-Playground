FROM microsoft/aspnetcore

ENV ASPNETCORE_URLS http://+:5000
EXPOSE 5000

COPY publish-output /app
WORKDIR /app

ENTRYPOINT ["dotnet", "app.dll"]

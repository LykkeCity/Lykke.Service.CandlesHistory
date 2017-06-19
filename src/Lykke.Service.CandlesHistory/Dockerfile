FROM microsoft/dotnet:1.1-runtime
ARG source
WORKDIR /app
COPY . .
ENTRYPOINT ["dotnet", "Lykke.Service.CandlesHistory.dll"]

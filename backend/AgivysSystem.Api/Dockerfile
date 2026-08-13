# 1. Estágio de Compilação
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia apenas o arquivo de projeto e restaura as dependências primeiro (otimiza cache do Docker)
COPY ["AgiVysSystem.csproj", "./"]
RUN dotnet restore "./AgiVysSystem.csproj"

# Copia o restante dos arquivos do projeto backend
COPY . .

# Compila o projeto em modo Release
RUN dotnet publish "./AgiVysSystem.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 2. Estágio de Execução
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# A aplicação está configurada no Program.cs para rodar na porta 5000: app.Urls.Add("http://0.0.0.0:5000");
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000

# Copia apenas os binários compilados do estágio anterior
COPY --from=build /app/publish .

# Define o ponto de entrada da aplicação
ENTRYPOINT ["dotnet", "AgiVysSystem.dll"]

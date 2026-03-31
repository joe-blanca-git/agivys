FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia o arquivo de projeto usando o caminho relativo à raiz do repo
COPY ["backend/AgivysSystem.Api/AgiVysSystem.csproj", "backend/AgivysSystem.Api/"]
RUN dotnet restore "backend/AgivysSystem.Api/AgiVysSystem.csproj"

# Copia todo o conteúdo da pasta backend para o container
COPY . .

# Muda para a pasta onde o projeto está para buildar
WORKDIR "/src/backend/AgivysSystem.Api"
RUN dotnet publish "AgiVysSystem.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

# Define a porta padrão
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# O nome da DLL costuma ser o mesmo do .csproj
ENTRYPOINT ["dotnet", "AgiVysSystem.dll"]
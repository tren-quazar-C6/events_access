# ---------- Stage 1: BUILD ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiar primero solo el csproj para aprovechar cache de Docker
# Si solo cambia el código y no las dependencias, este layer se reusa.
COPY *.csproj ./
RUN dotnet restore

# Copiar el resto del código y publicar
COPY . ./
RUN dotnet publish -c Release -o /out --no-restore

# ---------- Stage 2: RUNTIME ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Copiar solo lo compilado desde el stage de build
COPY --from=build /out ./

# ASP.NET Core 8+ escucha en 8080 por defecto
EXPOSE 8080

# El nombre del .dll debe coincidir con el .csproj de tu proyecto.
# Si tu proyecto se llama events_admin.csproj, el dll es events_admin.dll
ENTRYPOINT ["dotnet", "events_access.dll"]
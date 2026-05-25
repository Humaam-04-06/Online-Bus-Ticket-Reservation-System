# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY ["Second_Try.csproj", "./"]
RUN dotnet restore "Second_Try.csproj"

# Copy everything else and publish
COPY . .
RUN dotnet publish "Second_Try.csproj" -c Release -o /app/publish

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Render injects a dynamic $PORT. Microsoft ASP.NET Core 8+ supports HTTP_PORTS.
# We set a default of 8080 which matches Render's automatic port mapping.
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Second_Try.dll"]

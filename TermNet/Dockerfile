FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY TermNet/TermNet.csproj ./TermNet/
COPY Shared/Shared.csproj ./Shared/
RUN dotnet restore "./TermNet/TermNet.csproj"
COPY . .
RUN dotnet publish "./TermNet/TermNet.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./
ENV ASPNETCORE_URLS="http://+:8080"
EXPOSE 8080
ENTRYPOINT ["dotnet", "TermNet.dll"]

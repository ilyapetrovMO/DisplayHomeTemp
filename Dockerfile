FROM mcr.microsoft.com/dotnet/core/sdk:5.0 as BUILD
ARG DATABASE_URL
ARG ASPNETCORE_ENVIRONMENT
WORKDIR /src
COPY . .
RUN dotnet tool install dotnet-ef && dotnet publish -c Release -o ./publish

FROM mcr.microsoft.com/dotnet/core/aspnet:5.0 AS RUNTIME
WORKDIR /app
COPY --from=BUILD /src/publish .
RUN rm /bin/sh && ln -s /bin/bash /bin/sh
CMD ASPNETCORE_URLS=http://*:$PORT dotnet DisplayHomeTemp.dll
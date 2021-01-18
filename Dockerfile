FROM mcr.microsoft.com/dotnet/sdk:5.0 as BUILD
ARG DATABASE_URL
ARG ASPNETCORE_ENVIRONMENT
WORKDIR /src
COPY . .
RUN dotnet tool install dotnet-ef && dotnet publish -c Release -o ./publish

FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS RUNTIME
ENV VAPID_PUBLIC_KEY $VAPID_PUBLIC_KEY
ENV VAPID_PRIVATE_KEY $VAPID_PRIVATE_KEY
ENV VAPID_SUBJECT $VAPID_SUBJECT
WORKDIR /app
COPY --from=BUILD /src/publish .
RUN rm /bin/sh && ln -s /bin/bash /bin/sh
CMD dotnet ef database update
CMD ASPNETCORE_URLS=http://*:$PORT ASPNETCORE_ENVIRONMENT=Production dotnet DisplayHomeTemp.dll
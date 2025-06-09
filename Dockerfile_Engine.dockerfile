FROM mcr.microsoft.com/dotnet/sdk:6.0
WORKDIR /app
COPY . .
RUN dotnet restore
RUN dotnet build
dotnet test --logger "trx;logfilename=MyTestReport.trx" --results-directory MyTestResults
CMD ["dotnet", "run"]

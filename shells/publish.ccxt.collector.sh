cd ./tests/ocollector

supervisorctl stop ccxt.ocollector

dotnet restore -r ubuntu.18.04-x64

dotnet publish -c Release -r ubuntu.18.04-x64 -f netcoreapp3.1 -o /var/ccxt.ocollector

supervisorctl start ccxt.ocollector
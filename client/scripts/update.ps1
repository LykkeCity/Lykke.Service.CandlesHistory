cd ../Lykke.Service.CandlesHistory.Client/
iwr http://localhost:5000/swagger/v1/swagger.json -o Service.CandlesHistory.json
autorest --input-file=Service.CandlesHistory.json --csharp --namespace=Lykke.Service.CandlesHistory.Client --output-folder=./
rm -rf deploy

dotnet pack src/SlackBotNet/SlackBotNet.csproj --output ../../deploy --include-symbols
dotnet pack src/SlackBotNet.Matcher.Luis/SlackBotNet.Matcher.Luis.csproj --output ../../deploy --include-symbols

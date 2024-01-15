# Use the official .NET SDK image as a build stage
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /app

# Copy the project file and restore dependencies
COPY *.csproj .
RUN dotnet restore

# Copy the application code
COPY . .

# Build the application
RUN dotnet publish -c Release -o out

# Use the official ASP.NET Core runtime image as the final image
FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /app

# Copy the published application
COPY --from=build /app/out .

# Expose the ports the app will run on
EXPOSE 5000
EXPOSE 5001

# Define the command to run your application
ENTRYPOINT ["dotnet", "fuquizlearn_api.dll"]

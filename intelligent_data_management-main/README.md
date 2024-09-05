
# Intelligent Data Management Project

This project demonstrates the integration and management of multiple dockerized databases including PostgreSQL, MongoDB, and Cassandra, within a .NET application. 

## Description

This application is designed to showcase how to configure and connect to different types of databases within a single .NET environment.

## NOTE
This lunches the ASP-net.core into  production via the docker file!

### Dependencies

- .NET 8 SDK
- Docker
- PostgreSQL, MongoDB, Redis (Docker images are used in this project)

### Configuration

The project uses an `appsettings.json` file for configuration. Here is the structure:

```json
{
 {
  "ConnectionStrings": {
    "DefaultConnection": "Host=postgres;Port=5432;Username=postgres;Password=IKT453;Database=postgres;",
    "MongoDbConnection": "mongodb://mongoadmin:secret@mongodb:27017",
    "RedisConnection": "redis:6379,abortConnect=false"
  },
  
  
  "CsvData": {
    "Path": "Data/data.csv"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "AllowedHosts": "*"
}
```
This is important to note since you use these to connect to the `docker-compose.yml`
```  postgres:
    services:
  postgres:
    image: 'postgres:13.2'
    volumes:
      - 'db-data:/var/lib/postgresql/data'
    ports:
      - '5432:5432'
    environment:
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: IKT453
      POSTGRES_DB: employee

  redis:
    image: 'redis:latest'
    ports:
      - '6379:6379'
    volumes:
      - 'redis-data:/data'

  mongodb:
    image: 'mongo:latest'
    ports:
      - '27017:27017'
    volumes:
      - 'mongodb-data:/data/db'
    environment:
      MONGO_INITDB_ROOT_USERNAME: mongoadmin
      MONGO_INITDB_ROOT_PASSWORD: secret

```

### Setup for development

1. **Dotnet setup**
  - Make sure you have install dotnet 8 runtime and sdk
  - `https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-aspnetcore-5.0.17-windows-x64-installer?cid=getdotnetcore](https://dotnet.microsoft.com/en-us/download/dotnet/8.0`
2. **Make your changes to the project inside the `./site` folder**
3. **re-create the dockerfile**
  - while you are in the main `intelligent_data_management` directory use the `docker build -t dotnet-site .`
4. **Tag and push to docker hub**
  - `docker tag dotnet-site lemoi18/ikt435:latest` 
  - `docker push lemoi18/ikt435:latest`
  - make the necessary changes to the dockerhub to match your setup. EG: create your own dockerhub repo, and change `lemoi18/ikt435` to `yourname/yourrepo` in both
5. **Change the `docker-compose.yml`** 
  - here you want to change the dotnet-site image to your own like so
    ```
      dotnet:
    depends_on:
      - postgres
      - cassandra
      - mongodb
    build: .
    image: `yourname/yourrepo`
    ports:
      - '5000:5000'
    restart: always
    environment:
      - 'ASPNETCORE_URLS=http://+:5000'
      - ASPNETCORE_ENVIRONMENT=Production
    ```
  - Use `docker-compose up -d` to start the PostgreSQL, MongoDB,Cassandra, Kafka, zookeeper services.

6. **When you just want to start it up you can just do this**
   - Use `docker-compose up -d` to start the PostgreSQL, MongoDB,Cassandra, Kafka, zookeeper services.




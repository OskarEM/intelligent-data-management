@echo off

echo Building the Docker image...
docker build -t dotnet-site .
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

echo Tagging the image...
docker tag dotnet-site terrasankai/ikt435:latest

echo Pushing the image to the repository...
docker push terrasankai/ikt435:latest

echo Starting services...
docker-compose up -d

echo Deployment completed successfully.
pause
docker-compose down -v
@echo off
echo ?? Starting ASM-UET Production Deployment...

cd ASM-UET

echo ?? Cleaning previous builds...
dotnet clean --configuration Release

echo ?? Restoring NuGet packages...
dotnet restore

echo ?? Building application in Release mode...
dotnet build --configuration Release --no-restore

if %errorlevel% neq 0 (
    echo ? Build failed! Please fix errors before publishing.
    pause
    exit /b 1
)

echo ?? Publishing application for Production...
dotnet publish --configuration Release --output "bin\Release\net8.0\publish" --no-build

if %errorlevel% equ 0 (
    echo ? Application published successfully!
    echo ?? Published files location: %cd%\bin\Release\net8.0\publish
    echo ?? Environment: Production (set via web.config)
    echo.
    echo ?? Next steps:
    echo 1. Navigate to: %cd%\bin\Release\net8.0\publish
    echo 2. Zip all contents of the publish folder
    echo 3. Upload the zip file to your hosting provider
    echo 4. Extract on the server
) else (
    echo ? Publish failed!
    pause
    exit /b 1
)

cd ..

echo ?? Deployment preparation complete!
pause
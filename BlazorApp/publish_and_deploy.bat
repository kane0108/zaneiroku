@echo off
echo === PUBLISHING ===

set PROJECT_DIR=%~dp0
set PUBLISH_DIR=%PROJECT_DIR%publish
set OUTPUT_DIR=%PROJECT_DIR%docs

rem publish フォルダ削除
if exist "%PUBLISH_DIR%" rd /s /q "%PUBLISH_DIR%"

rem docs フォルダ作成/削除
if exist "%OUTPUT_DIR%" rd /s /q "%OUTPUT_DIR%"
mkdir "%OUTPUT_DIR%"

rem Blazor Publish
dotnet publish "%PROJECT_DIR%BlazorApp.csproj" -c Release -o "%PUBLISH_DIR%"

echo === COPYING TO docs/ ===

xcopy "%PUBLISH_DIR%\wwwroot\*" "%OUTPUT_DIR%\" /E /H /Y

echo === GIT COMMIT ===
git add .
git commit -m "Auto Deploy %date% %time%"

echo === PUSH ===
git push origin main --force

echo === DONE ===
pause

@echo off
echo === PUBLISHING ===

REM プロジェクトルート（batの場所）を基準に設定
set PROJECT_DIR=%~dp0

REM 出力先フォルダ削除
if exist "%PROJECT_DIR%publish" (
    rd /s /q "%PROJECT_DIR%publish"
)

REM Blazor Publish
dotnet publish "%PROJECT_DIR%BlazorApp.csproj" -c Release -o "%PROJECT_DIR%publish"

echo === COPYING TO ROOT ===

REM GitHub Pages は main のルートに配置するため、publish 内容をカレントにコピー
xcopy "%PROJECT_DIR%publish\*" "%PROJECT_DIR%" /E /H /Y

echo === GIT COMMIT ===

git add .
git commit -m "Auto Deploy %date% %time%"

echo === PUSH TO GITHUB (FORCE) ===

git push origin main --force

echo === DONE ===
pause

@echo off
setlocal

echo === PUBLISHING ===

REM ★ この bat があるフォルダを基準にする（最重要）
cd /d %~dp0

dotnet publish BlazorApp.csproj -c Release

echo === DEPLOYING ===

REM Publish 出力は bin\Release\net7.0\browser-wasm\publish_fuma_zanei\
xcopy "bin\Release\net7.0\browser-wasm\publish_fuma_zanei\*" "docs\" /e /y /i

git add .
git commit -m "Auto Deploy %date% %time%"
git push origin main

echo === DONE ===
pause

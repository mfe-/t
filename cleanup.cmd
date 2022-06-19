cd %cd%
rd /s /q .vs
rd /s /q packages
for /f "delims=" %%i in ('dir bin obj /s /b') do if exist "%%i" rd /s /q "%%i" 
rd /s /q TestResults
rem pause
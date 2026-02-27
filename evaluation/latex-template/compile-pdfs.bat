powershell -NoProfile -ExecutionPolicy Bypass -Command "(Get-Content .\sheet.tex) -replace '\\setboolean\{lsg\}\{.*\}', '\setboolean{lsg}{false}' | Set-Content .\sheet.tex"
latexmk -shell-escape -pdf -quiet -interaction=nonstopmode -jobname=sheet ./sheet.tex

powershell -NoProfile -ExecutionPolicy Bypass -Command "(Get-Content .\sheet.tex) -replace '\\setboolean\{lsg\}\{.*\}', '\setboolean{lsg}{true}' | Set-Content .\sheet.tex"
latexmk -shell-escape -pdf -quiet -interaction=nonstopmode -jobname=sheet-solution ./sheet.tex

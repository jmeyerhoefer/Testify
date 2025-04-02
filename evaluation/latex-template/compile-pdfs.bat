powershell -NoProfile -ExecutionPolicy Bypass -Command ^
    "(Get-Content .\sheet.tex) -replace '\\setboolean\{lsg\}\{.*\}', '\setboolean{lsg}{false}' | Set-Content .\sheet.tex"
latexmk -shell-escape -synctex=1 -interaction=nonstopmode -file-line-error -pdf ./sheet.tex

powershell -NoProfile -ExecutionPolicy Bypass -Command ^
    "(Get-Content .\sheet.tex) -replace '\\setboolean\{lsg\}\{.*\}', '\setboolean{lsg}{true}' | Set-Content .\sheet.tex"
latexmk -shell-escape -synctex=1 -interaction=nonstopmode -file-line-error -pdf -jobname=sheet-solution ./sheet.tex

#!/bin/bash

# Compile all files "name/name.tex" to "compiled/name.pdf".
# Continue on error, but exit with non-zero status at the end.

failed=0
failedjobs=""
newline=$'\n'

function compilePdf() {
    filename=$1
    outputname=$2
    echo "Compiling ${filename} to $outputname ..."
    latexmk -pdf -quiet -outdir=../compiled -jobname="$outputname" -e "\$pdflatex='pdflatex -interaction=nonstopmode';" "$file"
    if [ $? -ne 0 ]; then
        # Increment failed counter
        failed=$((failed + 1))
        failedjobs="${failedjobs}${newline}failed compiling $filename to $outputname"
    else
        # Remove log when successful
        rm -f "../compiled/${outputname}.log"
    fi
    echo "Done."
    echo ""
    echo ""
}



cd "template"    
compilePdf "thesis.tex" "thesis"
cd ..


if [ $failed -gt 0 ] ; then
    >&2 echo "Got $failed errors! See above for details."
    >&2 echo "$failedjobs"
    exit 1
fi

exit 0

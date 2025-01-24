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


for dir in */; do
    cd "$dir"
    file="${dir:0:${#dir}-1}.tex"
    if [ -f "$file" ] ; then
        jobname="${file%.*}"
        # turn solution off
        sed -i -e 's/\\setboolean{lsg}{.*}/\\setboolean{lsg}{false}/g' $file
        sed -i -e 's/\\setboolean{schema}{.*}/\\setboolean{schema}{false}/g' $file
	sed -i -e 's/\\setboolean{praesenzlsg}{.*}/\\setboolean{praesenzlsg}{false}/g' $file
        compilePdf $file $jobname
        if grep -q '\\setboolean{lsg}' $file; then
            # turn solution on
            sed -i -e 's/\\setboolean{lsg}{.*}/\\setboolean{lsg}{true}/g' $file
            compilePdf $file "$jobname-solution"
	    if grep -q '\\setboolean{praesenzlsg}' $file; then
		# turn praesenzlsg on
		sed -i -e 's/\\setboolean{praesenzlsg}{.*}/\\setboolean{praesenzlsg}{true}/g' $file
		compilePdf $file "$jobname-praesenz-solution"
		sed -i -e 's/\\setboolean{praesenzlsg}{.*}/\\setboolean{praesenzlsg}{false}/g' $file
	    fi
            if grep -q '\\setboolean{schema}' $file; then
                # turn schema on
                sed -i -e 's/\\setboolean{schema}{.*}/\\setboolean{schema}{true}/g' $file
                compilePdf $file "$jobname-schema"
            fi
        fi
    fi
    cd ..
done

if [ $failed -gt 0 ] ; then
    >&2 echo "Got $failed errors! See above for details."
    >&2 echo "$failedjobs"
    exit 1
fi

exit 0

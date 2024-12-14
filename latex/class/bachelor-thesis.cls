\NeedsTeXFormat{LaTeX2e}
\ProvidesClass{class/bachelor-thesis}[2024/10/24 LaTeX class for Bachelor Thesis]

\LoadClass[12pt]{report}

% paper format
\RequirePackage[a4paper, headheight=17pt, hmargin={15mm, 15mm}, vmargin={25mm, 20mm}]{geometry}

% NOT WORKING
% \RequirePackage{fontspec}
% \setmainfont{RedHatText}[
%     Path = font/static/,
%     Extension = .ttf,
%     UprightFont = *-Regular,
%     BoldFont = *-Bold,
%     ItalicFont = *-Italic,
%     BoldItalicFont = *-BoldItalic
% ]

% font, encoding
\RequirePackage{lmodern}
\renewcommand*{\familydefault}{\sfdefault}
\RequirePackage[T1]{fontenc}
\RequirePackage{iftex}
\ifXeTeX
\else
  \RequirePackage[utf8]{inputenc}
\fi

% float captions and section header formatting
\RequirePackage{caption}
\RequirePackage{subcaption}
\captionsetup{labelfont={bf,sf}}
\RequirePackage{titlesec}

% used packages
\RequirePackage{etoolbox}
\RequirePackage{graphicx}
\RequirePackage[svgnames]{xcolor}
\RequirePackage{soul}
\RequirePackage{hyperref}
\RequirePackage{graphicx}

\RequirePackage{svg}
\setsvg{inkscapeversion=1.4}

\RequirePackage{blindtext}
\RequirePackage{hyperref}
\RequirePackage{fancyhdr}
\RequirePackage{amsmath}
\RequirePackage{amssymb}

% \RequirePackage[backend=biber, style=numeric]{biblatex}
% \addbibresource{bibliography/bachelor-thesis.bib}

% hyperref setup
\hypersetup{
    colorlinks=true,
    urlcolor=blue,
    linkcolor=black
}

% title formatting
\def\@subtitle{\relax} 
\newcommand{\subtitle}[1]{\gdef\@subtitle{#1}}

% email formatting
\def\@email{\relax} 
\newcommand{\email}[1]{\gdef\@email{#1}}

\def\@abstract{} 
\renewcommand{\abstract}[1]{\gdef\@abstract{#1}}

\newcommand{\monthword}[1]{%
    \ifcase#1\or January\or
    February\or
    March\or
    April\or
    May\or
    June\or
    July\or
    August\or
    September\or
    October\or
    November\or
    December\fi%
}

% \newcommand{\maketitlepage}[1]{%
%     \begin{titlepage}
%         \begin{center}
%             % RPTU LOGO
%             { \includesvg[height=50px]{assets/logo/standard/rptu-logo} }
%             \vskip 4em
            
%             % TITLE
%             { \normalfont\sffamily\bfseries\Huge \@title: \par }
%             \vskip .5em
            
%             % SUBTITLE
%             { \normalfont\sffamily\mdseries\LARGE \@subtitle \par }
%             \vskip 2em
            
%             % BY
%             { \normalfont\sffamily\mdseries\large by \par }
%             \vskip 2em
            
%             % AUTHOR
%             { \normalfont\sffamily\mdseries\Large \@author \par }
%             \vskip .5em
            
%             % EMAIL
%             { \normalfont\sffamily\mdseries\Large
%                 Mail: \href{%
%                     mailto:\@email?
%                     subject=Bachelor Thesis
%                     &body=Add your question or remark}{\@email}
%                 \par }
%             \vskip 4em
            
%             % SUPERVISORS
%             { \normalfont\sffamily\mdseries\large
%                 \begin{align*}
%                     \textbf{Supervisors:}\quad
%                         & \text{Prof. Dr. Ralf Hinze}\\
%                         & \text{Mail: \href{mailto:ralf-hinze@cs.uni-kl.de}{ralf-hinze@cs.uni-kl.de}}\\[1em]
%                         & \text{Sebastian Schloßer, M.Sc. (Sebastian Schweizer)} \\
%                         & \text{Mail: \href{mailto:sebastian.schlosser@cs.rptu.de}{sebastian.schlosser@cs.rptu.de}}
%                 \end{align*} }
%             \vskip 5em
            
%             % TEXT
%             { \normalfont\sffamily\mdseries\large
%                 A dissertation submitted for the degree of Bachelor of Science in\\Computer Science.
%                 \par }
%             \vskip 3em
            
%             % DATE
%             { \normalfont\sffamily\mdseries\Large \the\day.~\monthword{\the\month}~\the\year \par }
%             \vfill
            
%             % COPYRIGHT
%             { \normalfont\sffamily\mdseries\large \copyright~Jakob Meyerhöfer, \the\year \par }
%         \end{center}
%         \newpage
%     \end{titlepage}%
% }

\newcommand{\maketitlepage}[1]{%
    \begin{titlepage}
        \begin{center}
            % RPTU LOGO
            { \includesvg[height=50px]{assets/logo/standard/rptu-logo} }
            \vfill

            % TITLE
            { \normalfont\sffamily\bfseries\Huge \@title: \par }
            \vskip 1em

            % SUBTITLE
            { \normalfont\sffamily\mdseries\LARGE \@subtitle \par }
            \vfill

            % AUTHOR
            { \normalfont\sffamily\mdseries\LARGE \@author \par }
            \vskip 1em

            % EMAIL
            { \normalfont\sffamily\mdseries\Large Mail: \href{mailto:\@email?subject=Bachelor Thesis&body=Add your question or remark}{\@email} \par }
            \vfill

            % TEXT
            { \normalfont\sffamily\mdseries\Large A dissertation submitted for the degree of Bachelor of Science in\\Computer Science. \par }
            \vskip 3em

            % DATE
            { \normalfont\sffamily\mdseries\Large \the\day.~\monthword{\the\month}~\the\year \par }
        \end{center}
        \newpage
    \end{titlepage}%
}

\fancypagestyle{abstractPagestyle}{
    \pagestyle{empty}
}

\fancypagestyle{mainPagestyle}{
    \pagestyle{fancy}
    \fancyhead[L]{\nouppercase\leftmark}
    \fancyhead[C]{\href{http://www.rptu.de/}{\includegraphics[height=1em]{assets/logo/minimal/rptu-logo-minimal11.png}}}
    \fancyhead[R]{\nouppercase\rightmark}
}
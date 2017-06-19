# SCR
Source Control Remover

## What it does
Source Control Remover removes Visual Source Safe information from the project.

## Why it does this.
I was switching version control software from Source Safe to Subversion and found it difficult because the Source Safe information was physically inside the project file.  I wrote Source Control Remover to recursively iterate folders and remove the lock file from each one, as well as edit the project files contained in the solution and remove the Source Safe lines of code from the XML.

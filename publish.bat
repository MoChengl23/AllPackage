

set /p mes=commit mes:  

echo %mes%
 
D:\my\Git\bin\git.exe add .

 
D:\my\Git\bin\git.exe commit -m "%mes%"

D:\my\Git\bin\git.exe subtree --prefix=Packages/FixedMath push git@github.com:MoChengl23/FixexMathPackage.git master

D:\my\Git\bin\git.exe push -u origin master


 


pause
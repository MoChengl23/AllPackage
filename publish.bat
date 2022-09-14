@echo off

set /p mes=commit mes:  

 
 
D:\my\Git\bin\git.exe add .

 
D:\my\Git\bin\git.exe commit -m "%mes%"

echo ------------push MoChengFixedMath------------
D:\my\Git\bin\git.exe subtree --prefix=Packages/MoChengFixedMath push git@github.com:MouseChannle/MoChengFixdMath.git master
echo ------------push MoChengHTTP------------
D:\my\Git\bin\git.exe subtree --prefix=Packages/MoChengHttp push git@github.com:MouseChannle/MoChengHTTP.git master

echo ------------push MoChengZip------------
D:\my\Git\bin\git.exe subtree --prefix=Packages/MoChengZip push git@github.com:MouseChannle/MoChengCompress.git master

echo ------------push MoChengClientNet------------
D:\my\Git\bin\git.exe subtree --prefix=Packages/MoChengClientNet push git@github.com:MouseChannle/MoChengClientNet.git master



D:\my\Git\bin\git.exe push -u origin master



pause
@echo off

set /p mes=commit mes:  

 
 
D:\my\Git\bin\git.exe add .

 
D:\my\Git\bin\git.exe commit -m "%mes%"

echo ------------push MoChengFixedMath------------
D:\my\Git\bin\git.exe subtree --prefix=Packages/MoChengFixedMath push git@github.com:MouseChannel/MoChengFixdMath.git master
echo ------------push MoChengHTTP------------
D:\my\Git\bin\git.exe subtree --prefix=Packages/MoChengHttp push git@github.com:MouseChannel/MoChengHTTP.git master

echo ------------push MoChengZip------------
D:\my\Git\bin\git.exe subtree --prefix=Packages/MoChengZip push git@github.com:MouseChannel/MoChengCompress.git master

echo ------------push MoChengClientNet------------
D:\my\Git\bin\git.exe subtree --prefix=Packages/MoChengClientNet push git@github.com:MouseChannel/MoChengClientNet.git master
echo ------------push MoChengRVO------------
 D:\my\Git\bin\git.exe subtree --prefix=Packages/MoChengRVO add git@github.com:MouseChannel/MoChengRVO.git master


D:\my\Git\bin\git.exe push -u origin master



pause
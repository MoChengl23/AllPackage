 
SET PackageName= ump_FixedMath
 
SET Version = 1.0.1
 
SET  Path= Packages\FixedMath

 
D:\my\Git\bin\git.exe subtree split -P %Path% --branch %PackageName%
 
D:\my\Git\bin\git.exe tag %Version% %PackageName%


D:\my\Git\bin\git.exe push origin %PackageName% %Version%
D:\my\Git\bin\git.exe push origin %PackageName%


pause
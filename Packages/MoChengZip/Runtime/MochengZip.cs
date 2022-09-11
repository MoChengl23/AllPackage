using System.Diagnostics;

using System;
using System.Collections.Generic;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;



namespace MoChengZip
{
    public static class MOChengZipUtil
    {
        /// <summary>
        /// compressDir ,must absolute dir
        /// </summary>
        /// <param name="srcDir">from, and must be a dir</param>
        /// <param name="zipPath"> to </param>
        /// <param name="OnCompressAction"></param>
        /// <param name="OnDoneAction"></param>
        public static void DirCompress(string srcDir, string zipPath, Action<int> OnCompressAction = null, Action OnDoneAction = null)
        {
            var fileList = GetAllFiles(srcDir);
            try
            {

                using (ZipOutputStream s = new ZipOutputStream(File.Create(zipPath)))
                {

                    foreach (var file in fileList)
                    {
                        SingleCompress(s, file.Item1, file.Item2);

                    }
                    s.Finish();
                    s.Close();

                }
            }
            catch (Exception e)
            {

            }
        }







        /// <summary>
        /// item1 = fullname, item2 = RelativePath
        /// </summary>
        /// <param name="srcDir"></param>
        private static List<(string, string)> GetAllFiles(string srcDir)
        {
            var fileList = new List<(string, string)>();
            var dirInfo = new DirectoryInfo(srcDir);
            var files = dirInfo.GetFiles("*", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                var file = files[i];

                int fullLength = file.FullName.Length;
                int dirLength = srcDir.Length;

                string fileRelativePath = file.FullName.Substring(dirLength, fullLength - dirLength);
                fileList.Add((file.FullName, fileRelativePath));


            }
            return fileList;


        }


        public static void SingleCompress(ZipOutputStream s, string srcPath, string zipEntryName)
        {
            var entry = new ZipEntry(zipEntryName);



            s.PutNextEntry(entry);

            using (FileStream fs = File.OpenRead(srcPath))
            {
                int sourceBytes;
                do
                {
                    byte[] buffer = new byte[4096];
                    sourceBytes = fs.Read(buffer, 0, buffer.Length);


                    s.Write(buffer, 0, sourceBytes);
                } while (sourceBytes > 0);
            }
        }


        /// <summary>
        /// compress a file
        /// </summary>
        /// <param name="s"></param>
        /// <param name="srcPath"></param>
        /// <param name="zipEntryName"></param>
        public static void SingleCompress(string srcPath, string zipPath, string zipEntryName = null)
        {
            try
            {

                using (ZipOutputStream s = new ZipOutputStream(File.Create(zipPath)))
                {

                    if (zipEntryName == null) zipEntryName = Path.GetFileName(srcPath);
                    var entry = new ZipEntry(zipEntryName);



                    s.PutNextEntry(entry);



                    using (FileStream fs = File.OpenRead(srcPath))
                    {
                        int sourceBytes;
                        do
                        {
                            byte[] buffer = new byte[4096];
                            sourceBytes = fs.Read(buffer, 0, buffer.Length);


                            s.Write(buffer, 0, sourceBytes);
                        } while (sourceBytes > 0);
                    }
                    s.Finish();
                    s.Close();
                }

            }
            catch (Exception e)
            {
                throw new Exception($"ComPress zip fail: srcfile = {srcPath}, zipFile = {zipPath},failMes  ={e}");
            }


        }


        public static void UnCompress(string zipPath, string toDirectionPath)
        {
            using (ZipInputStream s = new ZipInputStream(File.OpenRead(zipPath)))
            {

                ZipEntry theEntry;
                while ((theEntry = s.GetNextEntry()) != null)
                {

                    Console.WriteLine(theEntry.Name);

                    string directoryName = Path.GetDirectoryName(theEntry.Name);
                    string fileName = Path.GetFileName(theEntry.Name);




                    // if (Directory.Exists(toDirectionPath))
                    // {
                    //     Directory.Delete(toDirectionPath, true);
                    // }


                    string dirPath = toDirectionPath;

                    if (directoryName.Length > 0)
                    {
                        Directory.CreateDirectory(toDirectionPath + "/" + directoryName);
                        dirPath += "/" + directoryName;
                    }
                    else
                    {
                        Directory.CreateDirectory(toDirectionPath);
                    }

                    if (zipPath != String.Empty)
                    {
                        using (FileStream streamWriter = File.Create(dirPath + "/" + fileName))
                        {

                            int size = 2048;
                            byte[] data = new byte[2048];
                            while (true)
                            {
                                size = s.Read(data, 0, data.Length);
                                if (size > 0)
                                {
                                    streamWriter.Write(data, 0, size);
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
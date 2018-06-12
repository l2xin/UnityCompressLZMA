using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using UnityEngine;
using YZL.Compress.Info;

namespace YZL.Compress.UPK
{
    public class UPKFolder
    {

        /**  进度  **/
        public class CodeProgress
        {
            public ProgressDelegate m_ProgressDelegate = null;
            public CodeProgress(ProgressDelegate del)
            {
                m_ProgressDelegate = del;
            }

            public void SetProgress(Int64 inSize, Int64 outSize)
            {
            }

            public void SetProgressPercent(Int64 fileSize, Int64 processSize)
            {
                m_ProgressDelegate(fileSize, processSize);
            }
        }

        /*
         *文件信息类 
         */
        class OneFileInfor
        {
            public int id = 0;
            public int startPos = 0;
            public int size = 0;
            public int pathLength = 0;
            public string path = "";
            public byte[] data = null;
        };


        /**  异步打包一个文件夹  **/
        public static void PackFolderAsync(string inpath, string outpath, ProgressDelegate progress)
        {
            Thread packThread = new Thread(new ParameterizedThreadStart(PackFolder));
            FileChangeInfo info = new FileChangeInfo();
            info.inpath = inpath;
            info.outpath = outpath;
            info.progressDelegate = progress;
            packThread.Start(info);
        }


        /**  异步解包一个文件夹  **/
         public static void UnPackFolderAsync(string inpath, string outpath, ProgressDelegate progress)
         {
             Thread unpackThread = new Thread(new ParameterizedThreadStart(UnPackFolder));
             FileChangeInfo info = new FileChangeInfo();
             info.inpath = inpath;
             info.outpath = outpath;
             info.progressDelegate = progress;
             unpackThread.Start(info);
         }


        /**  同步打包一个文件夹  **/
        private static void PackFolder(object obj)
        {
            FileChangeInfo pathinfo = (FileChangeInfo)obj;
            string inpath = pathinfo.inpath;
            string outpath = pathinfo.outpath;
            CodeProgress progress = null;
            if (pathinfo.progressDelegate != null)
                progress = new CodeProgress(pathinfo.progressDelegate);

            int id = 0;
            int totalSize = 0;
            Dictionary<int, OneFileInfor> allFileInfoDic = new Dictionary<int, OneFileInfor>();

            /**  遍历一个文件夹的所有文件  **/

            Debug.Log("遍历文件夹 " + inpath);

            string sourceDirpath = inpath.Substring(0, inpath.LastIndexOf('/'));

            /** 读取文件夹下面所有文件的信息 **/
            DirectoryInfo dirInfo = new DirectoryInfo(inpath); //创建子目录

            foreach (FileInfo fileinfo in dirInfo.GetFiles("*.*", SearchOption.AllDirectories))
            {
                //如果拓展名为.meta表示为unity为每个资源生成的标识文件.
                if (fileinfo.Extension == ".meta")
                {
                    continue;
                }

                //规范化相对路径
                string filename = fileinfo.FullName.Replace("\\", "/");
                filename = filename.Replace(sourceDirpath + "/", "");

                int filesize = (int)fileinfo.Length;

                Debug.Log(id + " : " + filename + " 文件大小: " + filesize);

                OneFileInfor info = new OneFileInfor();
                info.id = id;
                info.size = filesize;
                info.path = filename;
                info.pathLength = new UTF8Encoding().GetBytes(filename).Length;

                /**  读取这个文件  **/
                FileStream fileStreamRead = new FileStream(fileinfo.FullName, FileMode.Open, FileAccess.Read);
                if (fileStreamRead == null)
                {
                    Debug.Log("读取文件失败 ： " + fileinfo.FullName);
                    return;
                }
                else
                {
                    byte[] filedata = new byte[filesize];
                    fileStreamRead.Read(filedata, 0, filesize);
                    info.data = filedata;
                }
                fileStreamRead.Close();


                allFileInfoDic.Add(id, info);

                id++;
                totalSize += filesize;
            }

            /**  遍历一个文件夹的所有文件 结束  **/

            Debug.Log("文件数量 : " + id);
            Debug.Log("文件总大小 : " + totalSize);

            /**  UPK中前面是写每个包的ID,StartPos,size,pathLength,path.
            /**  更新文件在UPK中的起始点  **/
            int firstfilestartpos = 0 + 4;
            for (int index = 0; index < allFileInfoDic.Count; index++)
            {
                firstfilestartpos += 4 + 4 + 4 + 4 + allFileInfoDic[index].pathLength;
            }

            int startpos = 0;
            for (int index = 0; index < allFileInfoDic.Count; index++)
            {
                if (index == 0)
                {
                    startpos = firstfilestartpos;
                }
                else
                {
                    startpos = allFileInfoDic[index - 1].startPos + allFileInfoDic[index - 1].size;//上一个文件的开始+文件大小;
                }

                allFileInfoDic[index].startPos = startpos;
            }

            /**  写文件  **/
            FileStream fileStream = new FileStream(outpath, FileMode.Create);

            /**  文件总数量  **/
            byte[] totaliddata = System.BitConverter.GetBytes(id);
            fileStream.Write(totaliddata, 0, totaliddata.Length);

            for (int index = 0; index < allFileInfoDic.Count; index++)
            {
                /** 写入ID **/
                byte[] iddata = System.BitConverter.GetBytes(allFileInfoDic[index].id);
                fileStream.Write(iddata, 0, iddata.Length);

                /**  写入StartPos  **/
                byte[] startposdata = System.BitConverter.GetBytes(allFileInfoDic[index].startPos);
                fileStream.Write(startposdata, 0, startposdata.Length);

                /**  写入size  **/
                byte[] sizedata = System.BitConverter.GetBytes(allFileInfoDic[index].size);
                fileStream.Write(sizedata, 0, sizedata.Length);

                /**  写入pathLength  **/
                byte[] pathLengthdata = System.BitConverter.GetBytes(allFileInfoDic[index].pathLength);
                fileStream.Write(pathLengthdata, 0, pathLengthdata.Length);

                /**  写入path  **/
                byte[] mypathdata = new UTF8Encoding().GetBytes(allFileInfoDic[index].path);

                fileStream.Write(mypathdata, 0, mypathdata.Length);
            }

            /**  写入文件数据  **/
            int totalprocessSize = 0;
            foreach (var infopair in allFileInfoDic)
            {
                OneFileInfor info = infopair.Value;
                int size = info.size;
                byte[] tmpdata = null;
                int processSize = 0;
                while (processSize < size)
                {
                    if (size - processSize < 1024)
                    {
                        tmpdata = new byte[size - processSize];
                    }
                    else
                    {
                        tmpdata = new byte[1024];
                    }
                    fileStream.Write(info.data, processSize, tmpdata.Length);

                    processSize += tmpdata.Length;
                    totalprocessSize += tmpdata.Length;
                    if(progress != null)
                        progress.SetProgressPercent(totalSize, totalprocessSize);
                }
            }
            fileStream.Flush();
            fileStream.Close();
            Debug.Log("打包完成");

        }

        public static void PackFolder(string inpath, string outpath, ProgressDelegate progress)
        {
            FileChangeInfo pathinfo = new FileChangeInfo();
            pathinfo.inpath = inpath;
            pathinfo.outpath = outpath;
            pathinfo.progressDelegate = progress;

            PackFolder(pathinfo);
        }


        /** 同步解包一个文件夹 **/
        private static void UnPackFolder(object obj)
        {

            FileChangeInfo pathinfo = (FileChangeInfo)obj;
            string inpath = pathinfo.inpath;
            string outpath = pathinfo.outpath;
            CodeProgress progress = null;
            if (pathinfo.progressDelegate != null)
                progress = new CodeProgress(pathinfo.progressDelegate);

            Dictionary<int, OneFileInfor> allFileInfoDic = new Dictionary<int, OneFileInfor>();

            System.Text.UTF8Encoding utf8Encoding = new System.Text.UTF8Encoding();


            int totalsize = 0;

            FileStream upkFilestream = new FileStream(inpath, FileMode.Open);
            upkFilestream.Seek(0, SeekOrigin.Begin);

            int offset = 0;

            //读取文件数量;
            byte[] totaliddata = new byte[4];
            upkFilestream.Read(totaliddata, 0, 4);
            int filecount = BitConverter.ToInt32(totaliddata, 0);
            offset += 4;
            Debug.Log("filecount=" + filecount);

            //读取所有文件信息;
            for (int index = 0; index < filecount; index++)
            {
                //读取id;
                byte[] iddata = new byte[4];
                upkFilestream.Seek(offset, SeekOrigin.Begin);
                upkFilestream.Read(iddata, 0, 4);
                int id = BitConverter.ToInt32(iddata, 0);
                offset += 4;

                //读取StartPos;
                byte[] startposdata = new byte[4];
                upkFilestream.Seek(offset, SeekOrigin.Begin);
                upkFilestream.Read(startposdata, 0, 4);
                int startpos = BitConverter.ToInt32(startposdata, 0);
                offset += 4;

                //读取size;
                byte[] sizedata = new byte[4];
                upkFilestream.Seek(offset, SeekOrigin.Begin);
                upkFilestream.Read(sizedata, 0, 4);
                int size = BitConverter.ToInt32(sizedata, 0);
                offset += 4;

                //读取pathLength;
                byte[] pathLengthdata = new byte[4];
                upkFilestream.Seek(offset, SeekOrigin.Begin);
                upkFilestream.Read(pathLengthdata, 0, 4);
                int pathLength = BitConverter.ToInt32(pathLengthdata, 0);
                offset += 4;

                //读取path;
                byte[] pathdata = new byte[pathLength];
                upkFilestream.Seek(offset, SeekOrigin.Begin);
                upkFilestream.Read(pathdata, 0, pathLength);
                string path = utf8Encoding.GetString(pathdata);
                offset += pathLength;


                //添加到Dic;
                OneFileInfor info = new OneFileInfor();
                info.id = id;
                info.size = size;
                info.pathLength = pathLength;
                info.path = path;
                info.startPos = startpos;
                allFileInfoDic.Add(id, info);

                totalsize += size;

                Debug.Log("id=" + id + " startPos=" + startpos + " size=" + size + " pathLength=" + pathLength + " path=" + path);
            }



            //释放文件;
            int totalprocesssize = 0;
            foreach (var infopair in allFileInfoDic)
            {
                OneFileInfor info = infopair.Value;

                int startPos = info.startPos;
                int size = info.size;
                string path = info.path;

                //创建文件;
                string dirpath = outpath + path.Substring(0, path.LastIndexOf('/'));
                string filepath = outpath + path;
                //判断文件夹是否存在
                if (Directory.Exists(dirpath) == false)
                {
                    Directory.CreateDirectory(dirpath);
                }
                //判断如果已经有文件了就删除，再生成.(相当于替换)
                if (File.Exists(filepath))
                {
                    File.Delete(filepath);
                }

                FileStream fileStream = new FileStream(filepath, FileMode.Create);

                byte[] tmpfiledata;
                int processSize = 0;
                while (processSize < size)
                {
                    if (size - processSize < 1024)
                    {
                        tmpfiledata = new byte[size - processSize];
                    }
                    else
                    {
                        tmpfiledata = new byte[1024];
                    }

                    //读取;
                    upkFilestream.Seek(startPos + processSize, SeekOrigin.Begin);
                    upkFilestream.Read(tmpfiledata, 0, tmpfiledata.Length);

                    //写入;
                    fileStream.Write(tmpfiledata, 0, tmpfiledata.Length);

                    processSize += tmpfiledata.Length;
                    totalprocesssize += tmpfiledata.Length;
                    if (progress != null)
                        progress.SetProgressPercent((long)totalsize, (long)totalprocesssize);
                }
                fileStream.Flush();
                fileStream.Close();
            }
            upkFilestream.Close();
            Debug.Log("解包完成");
        }

        public static void UnPackFolder(string inpath, string outpath, ProgressDelegate progress)
        {
            FileChangeInfo pathinfo = new FileChangeInfo();
            pathinfo.inpath = inpath;
            pathinfo.outpath = outpath;
            pathinfo.progressDelegate = progress;

            UnPackFolder(pathinfo);
        }

    }
}


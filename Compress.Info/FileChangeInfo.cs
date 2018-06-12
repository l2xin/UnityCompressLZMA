using System;

namespace YZL.Compress.Info
{
    public delegate void ProgressDelegate(Int64 fileSize, Int64 processSize);
    public class FileChangeInfo
    {
        public string inpath;
        public string outpath;
        public ProgressDelegate progressDelegate;
    };
}


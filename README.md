# UnityCompressLZMA
UPK->LZMA压缩

LZMA压缩方式：Compress.Info.dll和Compress.LZMA.dll 文件   

GZip压缩方式：Compress.Info.dll和ICSharpCode.SharpZipLib.dll 文件

 LZMAFile.CompressAsync(Application.dataPath + "/music.mp3", Application.dataPath + "/music.lzma", null);  
 GZipFile.CompressAsync(Application.dataPath + "/music.mp3", Application.dataPath + "/music.gzip", null);  
 LZMAFile.DeCompressAsync(Application.dataPath + "/music.lzma", Application.dataPath + "/lzmamusic.mp3", null);  
 GZipFile.DeCompressAsync(Application.dataPath + "/music.gzip", Application.dataPath + "/gzipmusic.mp3", null);
 
 UPKFolder.PackFolderAsync(Application.dataPath + "/picture", Application.dataPath + "/picture.upk", ShowProgress);  
 UPKFolder.UnPackFolderAsync(Application.dataPath + "/picture.upk", Application.dataPath + "/", ShowProgress);  
 
 void ShowProgress(long all,long now)  {  
     double progress = (double)now /all;  
     Debug.Log("当前进度为: " + progress);  
}  

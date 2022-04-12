using System;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Bot.Common.Trivial;
using DbEntity;

namespace Bot.Common.ImageHelper
{
	public static class ShortcutImageHelper
	{
        private static SynableImageHelper _helper;
        static ShortcutImageHelper()
        {
            _helper = new SynableImageHelper(TransferFileTypeEnum.ShortcutImage);
        }
        public static bool CopyTo(string imageName, string path)
        {
            return _helper.CopyTo(imageName, path);
        }
        public static string GetFullPath(string imageName)
        {
            return _helper.GetFullName(imageName);
        }
        public static string DownloadImage(string imageName)
        {
            return _helper.DownloadImage(imageName);
        }
        public static void UseImage(string imageName, Action<BitmapImage> act)
        {
            _helper.UseImage(imageName, act);
        }
        public static void UseImage(string imageName, Image image_0)
        {
            _helper.UseImage(imageName, image_0);
        }
        public static void DeleteImage(string imageName)
        {
            _helper.DeleteImage(imageName);
        }
        public static string AddNewImage(string imageName, string parnFnOld)
        {
            return _helper.AddNewImage(imageName, parnFnOld);
        }
        public static string AddNewImageForFullPath(string fullFn)
        {
            return _helper.AddNewImage(fullFn);
        }
        public static void CopyImageIntoCache(string fn)
        {
            _helper.CopyImageIntoCache(fn);
        }
        public static void UploadImageIfServerHaveNot(string partFn)
        {
            _helper.UploadImageIfServerHaveNot(partFn);
        }
        public static string AddNewImage(string imageName)
        {
            string rt = null;
            if (!string.IsNullOrEmpty(imageName))
            {
                string fullFn = GetFullPath(imageName);
                rt = AddNewImageForFullPath(fullFn);
            }
            return rt;
        }
	}
}

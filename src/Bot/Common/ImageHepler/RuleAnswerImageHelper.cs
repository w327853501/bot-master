using Bot.Common.Trivial;
using BotLib.Extensions;
using DbEntity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace Bot.Common.ImageHelper
{
    public static class RuleAnswerImageHelper
    {
        private static SynableImageHelper _helper;
        static RuleAnswerImageHelper()
        {
            _helper = new SynableImageHelper(TransferFileTypeEnum.RuleAnswerImage);
        }

        public static bool CopyTo(string src, string dest)
        {
            return _helper.CopyTo(src, dest);
        }

        internal static string GetFullName(string fn)
        {
            return _helper.GetFullName(fn);
        }

        public static string DownloadImage(string fn)
        {
            return _helper.DownloadImage(fn);
        }

        public static void UseImage(string fn, Action<BitmapImage> act)
        {
            _helper.UseImage(fn, act);
        }

        public static BitmapImage UseImage(string imageName)
        {
            return _helper.UseImage(imageName);
        }

        public static void UseImage(string fn, Image image)
        {
            _helper.UseImage(fn, image);
        }

        public static void DeleteImage(string imageName)
        {
            _helper.DeleteImage(imageName);
        }

        public static string AddNewImage(string fn, string parnFnOld)
        {
            return _helper.AddNewImage(fn, parnFnOld);
        }

        public static string AddNewImage(string fullFn)
        {
            return _helper.AddNewImage(fullFn);
        }

        public static void CopyImageIntoCache(string fn)
        {
            _helper.CopyImageIntoCache(fn);
        }
        public static void DeleteImages(List<string> imgs)
        {
            if (!imgs.xIsNullOrEmpty())
            {
                foreach (var imgName in imgs)
                {
                    DeleteImage(imgName);
                }
            }
        }

        public static bool IsTwoImageEqual(string imgpath1, string imgpath2)
        {
            var rt = false;
            if (string.IsNullOrEmpty(imgpath1))
            {
                rt = string.IsNullOrEmpty(imgpath2);
            }
            else if (string.IsNullOrEmpty(imgpath2))
            {
                rt = false;
            }
            else
            {
                DownloadImage(imgpath1);
                DownloadImage(imgpath2);
                rt = FileEx.IsTwoFileEqual(imgpath1, imgpath2);
            }
            return rt;
        }
    }
}

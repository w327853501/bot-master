using Bot.Common.Ntfs;
using Bot.Common.Windows;
using BotLib;
using BotLib.Cypto;
using BotLib.Extensions;
using BotLib.Wpf.Extensions;
using PeNet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Common
{
    public class HookQn
    {
        //public static async Task OpenHookQnAsync()
        //{
        //    await Task.Run(()=> {
        //        try
        //        {
        //            DispatcherEx.xInvoke(() => WndLoading.ShowWaiting());
        //            var qnExe = Params.Booter.GetAliWorkbenchExePath();
        //            if (string.IsNullOrEmpty(qnExe))
        //            {
        //                var allFiles = new MFTScanner().EnumerateFiles(new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)).Root.FullName);
        //                var qnLnk = allFiles.FirstOrDefault(k => k.EndsWith("千牛工作台.lnk"));
        //                var shell = new WshShell();
        //                var shortcut = (IWshShortcut)shell.CreateShortcut(qnLnk);                        
        //                qnExe = shortcut.TargetPath;
        //            }
        //            var qnPID = 0;
        //            string channelName = null;
        //            var injectLib = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "CefHook.dll");
        //            EasyHook.RemoteHooking.IpcCreateServer<CefHook.ServerInterface>(ref channelName, System.Runtime.Remoting.WellKnownObjectMode.Singleton);

        //            EasyHook.RemoteHooking.CreateAndInject(
        //                        qnExe,          // executable to run
        //                        "",                 // command line arguments for target
        //                        0,                  // additional process creation flags to pass to CreateProcess
        //                        EasyHook.InjectionOptions.DoNotRequireStrongName, // allow injectionLibrary to be unsigned
        //                        injectLib,   // 32-bit library to inject (if target is 32-bit)
        //                        injectLib,   // 64-bit library to inject (if target is 64-bit)
        //                        out qnPID,      // retrieve the newly created process ID
        //                        channelName         // the parameters to pass into injected library
        //                    );
        //        }
        //        catch (Exception ex)
        //        {
        //            Log.Exception(ex);
        //            var qnPath = WndInput.MyShowDialog("查找千牛启动程序错误，请手动设置千牛启动路径...","提示", @"C:\Program Files (x86)\AliWorkbench\AliWorkbench.exe", string.Empty,txt=> {
        //                if (string.IsNullOrEmpty(txt))
        //                {
        //                    return "必填";
        //                }
        //                return null;
        //            });
        //            Params.Booter.SetAliWorkbenchExePath(qnPath);
        //        }
        //        finally
        //        {
        //            DispatcherEx.xInvoke(() => WndLoading.CloseWaiting());
        //        }
        //    });
        //}


        public static void PatchQnIat()
        {
            var aliWorkbenchPs = Process.GetProcessesByName("AliWorkbench").FirstOrDefault();            
            //检测是否已经被Hook
            if (aliWorkbenchPs != null && !aliWorkbenchPs.Modules.Cast<ProcessModule>().Any(x => x.ModuleName == "AefHook.dll"))
            {
                var aliWorkbenchExePath = aliWorkbenchPs?.Modules.Cast<ProcessModule>().Where(x => x.ModuleName == "AliWorkbench.exe").First().FileName;
                if(MsgBox.ShowDialogEx("亲，由于您当前使用的千牛版本暂时无法兼容，请您使用可适配的版本，点击下方按钮重启即可！！！","提示"))
                {
                    Process.GetProcessesByName("AliWorkbench").ToList().ForEach(p=>p.Kill());
                    Util.WaitFor(() => {
                        return Process.GetProcessesByName("AliWorkbench").Length < 1;
                    },2000,100);
                    var srcAefHook = Path.Combine(PathEx.StartUpPathOfExe, "AefHook.dll");
                    var destAefHook = Path.Combine(Path.GetDirectoryName(aliWorkbenchExePath), "AefHook.dll");
                    File.Copy(srcAefHook, destAefHook,true);
                    var peFile = new PeFile(aliWorkbenchExePath);
                    // add function StartPage of the gdi32.dll to the import table of the opened .exe file inside memory. It doesn't write changes to disk.
                    peFile.AddImport("AefHook.dll", "HookCef");
                    // save changes to disk
                    System.IO.File.WriteAllBytes(aliWorkbenchExePath, peFile.RawFile.ToArray());
                    Process.Start(aliWorkbenchExePath);
                }
            }
        }
    }
}

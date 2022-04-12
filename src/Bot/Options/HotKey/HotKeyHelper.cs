using BotLib;
using BotLib.Extensions;
using BotLib.Wpf.Extensions;
using Bot.AssistWindow;
using Bot.AssistWindow.NotifyIcon;
using Bot.Automation;
using Bot.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;

namespace Bot.Options.HotKey
{
    public class HotKeyHelper
	{
		public static bool IsDownKeyRegistered;
		private KeyboardHook _hook;
		private static object _synobj;

		static HotKeyHelper()
		{
			IsDownKeyRegistered = false;
			_synobj = new object();
		}

		public static void Dispatch(IntPtr op)
		{
			try
			{
				var wndAssist = WndAssist.GetTopWindow();
				if (wndAssist != null)
				{
					wndAssist.ctlBottomPanel.MonitorHotKey((HotOp)((int)op));
				}
			}
			catch (Exception ex)
			{
				Log.Exception(ex);
				MsgBox.ShowErrTip(ex.Message,null);
			}
		}

		public void HookDownKey()
		{
			try
			{
				if (_hook != null)
				{
					UnHookDownKey();
				}
				_hook = new KeyboardHook();
                _hook.KeyDownEvent = (sender,e) =>
                {

                };
				_hook.Start();
			}
			catch (Exception e)
			{
				Log.Exception(e);
			}
		}

		public static string Init()
		{
			var tipMsg = string.Empty;
			try
			{
                RegisterDownKey();
				Keys keys;
				if (Params.HotKey.GetHotKey(HotOp.QinKong, out keys) && !Register(keys, HotOp.QinKong))
				{
					tipMsg += "清空,";
				}
				if (Params.HotKey.GetHotKey(HotOp.ZhiSi, out keys) && !Register(keys, HotOp.ZhiSi))
				{
					tipMsg += "知识,";
				}
				if (tipMsg.Length > 0)
				{
					tipMsg = tipMsg.Substring(0, tipMsg.Length - 1);
					tipMsg = string.Format("“{0}”等功能的“快捷键”失效，原因是这些快捷键已经被其它程序占用。", tipMsg);
				}
			}
			catch (Exception ex)
			{
				Log.Exception(ex);
				tipMsg += ex.Message;
			}
			return tipMsg;
		}

		public static bool Register(Keys keys, HotOp op)
		{
			bool rt = false;
			try
			{
                rt = WindowsShell.RegisterHotKey(WndNotifyIcon.Inst, keys, (int)op);
			}
			catch (Exception e)
			{
				Log.Exception(e);
			}
			return rt;
		}

        public static void RegisterDownKey()
		{
			if (Params.InputSuggestion.IsUseDownKey)
			{
				TryRegisterDownKey();
			}
			else
			{
				TryUnRegisterDownKey();
			}
		}

        public static string GetHotKeyDesc(Keys key)
		{
			var evt = new System.Windows.Forms.KeyEventArgs(key);
			string text = "";
			string textCode = "";
			string hotKeyDesc = "无";
			if (evt.KeyCode != Keys.Back && evt.KeyCode != Keys.Space && evt.KeyCode != Keys.Delete)
			{
				if (evt.Control)
				{
					text = "Ctrl";
				}
				if (evt.Shift)
				{
					text += ((text == "") ? "Shift" : " + Shift");
				}
				if (evt.Alt)
				{
					text += ((text == "") ? "Alt" : " + Alt");
				}
				if (evt.KeyCode != Keys.ShiftKey && evt.KeyCode != Keys.ControlKey && evt.KeyCode != Keys.Menu)
				{
					textCode = evt.KeyCode.ToString();
				}
				hotKeyDesc = ((text == "") ? textCode : (text + " + " + textCode));
			}
			return hotKeyDesc;
		}


        public static void UnRegister(HotOp op)
		{
			WindowsShell.UnregisterHotKey(WndNotifyIcon.Inst, (int)op);
		}

		public static bool TryRegisterDownKey()
		{
			return LockEx.TryLock(_synobj, 100, ()=>{
                if (!IsDownKeyRegistered)
				{
					try
					{
						Register(Keys.Down, HotOp.ArrowDown);
                        Register(Keys.Back | Keys.Space | Keys.Control, HotOp.ArrowDown2);
						IsDownKeyRegistered = true;
					}
					catch (Exception e)
					{
						Log.Exception(e);
					}
				}
            });
		}

		public static bool TryUnRegisterDownKey()
		{
			bool rt;
			if (!(rt = LockEx.TryLockMultiTime(_synobj, 100, ()=>{
                if (IsDownKeyRegistered)
				{
					IsDownKeyRegistered = false;
					try
					{
                        UnRegister(HotOp.ArrowDown);
                        UnRegister(HotOp.ArrowDown2);
					}
					catch (Exception e)
					{
						Log.Exception(e);
					}
				}
            }, 5, 10)))
			{
				Log.Error("TryUnRegisterDownKey Failed.");
			}
			return rt;
		}

		public void UnHookDownKey()
		{
			try
			{
                if (_hook != null)
				{
                    _hook.Stop();
				}
			}
			catch (Exception e)
			{
				Log.Exception(e);
			}
		}

		public static string QinKongHotKeyDesc
		{
			get
			{
				Keys keys;
                return Params.HotKey.GetHotKey(HotOp.QinKong, out keys) ? GetHotKeyDesc(keys) : "无";
			}
		}

		public static string ZhiSiHotKeyDesc
		{
			get
			{
				Keys keys;
                return Params.HotKey.GetHotKey(HotOp.ZhiSi, out keys) ? GetHotKeyDesc(keys) : "无";
			}
		}

		public enum HotOp
		{
			QinKong,
			ZhiSi,
			Unknown,
			ArrowDown,
			ArrowDown2
		}

		public class WindowsShell
		{
			public static int MOD_ALT;
			public static int MOD_CONTROL;
			public static int MOD_SHIFT;
			public static int MOD_WIN;
			public static int WM_HOTKEY;

			static WindowsShell()
			{
				MOD_ALT = 1;
				MOD_CONTROL = 2;
				MOD_SHIFT = 4;
				MOD_WIN = 8;
				WM_HOTKEY = 786;
			}

			[DllImport("user32.dll")]
			private static extern bool RegisterHotKey(int hWnd, int id, int fsModifiers, int vk);

            public static bool RegisterHotKey(Window f, Keys key, int keyid)
			{
				var isok = false;
				DispatcherEx.xInvoke(()=>{
                    isok = RegisterHotKey(f.xHandle(), keyid, 0, (int)key);
                    if (!isok)
                    {
                        var isInvalid = false;
                        var lastErr = WinApi.GetLastError(out isInvalid);
                        Log.Error(string.Format("RegisterHotKey失败，keyid={0},GetLastError={1},,isInvalid={2}", keyid, lastErr, isInvalid));
                    }
                });
                return isok;
			}

			[DllImport("user32.dll")]
			private static extern bool UnregisterHotKey(int hWnd, int id);

			public static bool UnregisterHotKey(Window f, int keyid)
			{
				var isok = false;
				DispatcherEx.xInvoke(()=>{
                    try 
	                {
                        isok = UnregisterHotKey(f.xHandle(), keyid);
                        if(!isok){
                            var isInvalid = false;
                            var lastErr = WinApi.GetLastError(out isInvalid);
                            Log.Error(string.Format("UnregisterHotKey失败，keyid={0},GetLastError={1},,isInvalid={2}",keyid,lastErr,isInvalid));
                        }
	                }
	                catch (Exception ex)
	                {
                        Log.Exception(ex);
	                }
                });
                return isok;
			}
		}
    }
}
